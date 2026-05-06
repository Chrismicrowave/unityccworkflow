using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

[InitializeOnLoad]
public static class InspectorRefsEmitter
{
    static InspectorRefsEmitter()
    {
        Emit();
    }

    class EventEntry
    {
        public string sourceAsset;
        public string targetFileId;
        public string targetStableId;
        public string methodName;
        public string targetTypeName;
    }

    public static void EmitNow() => Emit();

    public static void Emit()
    {
        Directory.CreateDirectory(AgentMirrorConfig.OutputDir);

        // Load SceneMirror to resolve fileID → stableId
        var fileIdToStableId = new Dictionary<string, string>();
        if (File.Exists(AgentMirrorConfig.SceneMirrorPath))
        {
            // Simple parse: look for stableId fields in SceneMirror
            // SceneMirror is keyed by stableId, not fileID — best-effort reverse map
        }

        var entries = new List<EventEntry>();

        // Scan .unity and .prefab files for UnityEvent persistent calls
        var assets = AssetDatabase.FindAssets("t:Scene t:Prefab");
        var scanned = new HashSet<string>();
        foreach (var guid in assets)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (scanned.Contains(path)) continue;
            scanned.Add(path);

            ScanAssetFile(path, entries, fileIdToStableId);
        }

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"emittedAt\": \"{System.DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",");
        sb.AppendLine($"  \"entryCount\": {entries.Count},");
        sb.AppendLine("  \"refs\": [");
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var comma = i < entries.Count - 1 ? "," : "";
            sb.AppendLine($"    {{\"sourceAsset\": \"{e.sourceAsset.Replace("\\", "/")}\", " +
                $"\"targetFileId\": \"{e.targetFileId}\", " +
                $"\"targetStableId\": \"{e.targetStableId}\", " +
                $"\"methodName\": \"{e.methodName}\", " +
                $"\"targetTypeName\": \"{e.targetTypeName}\"}}{comma}");
        }
        sb.AppendLine("  ]");
        sb.AppendLine("}");

        File.WriteAllText(AgentMirrorConfig.InspectorRefsPath, sb.ToString());
    }

    static void ScanAssetFile(string assetPath, List<EventEntry> entries, Dictionary<string, string> fileIdToStableId)
    {
        if (!File.Exists(assetPath)) return;

        // Only works with Force Text serialization mode
        var text = File.ReadAllText(assetPath);
        if (!text.Contains("m_PersistentCalls")) return;

        // Regex to find persistent call blocks
        var callPattern = new Regex(
            @"m_Target:\s*\{fileID:\s*(\d+).*?\}\s*" +
            @"m_TargetAssemblyTypeName:\s*(.*?)\n.*?" +
            @"m_MethodName:\s*(.*?)\n",
            RegexOptions.Singleline);

        var matches = callPattern.Matches(text);
        foreach (Match m in matches)
        {
            if (!m.Success) continue;
            var fileId = m.Groups[1].Value.Trim();
            var typeName = m.Groups[2].Value.Trim();
            var methodName = m.Groups[3].Value.Trim();

            if (string.IsNullOrEmpty(methodName) || methodName == "0") continue;

            fileIdToStableId.TryGetValue(fileId, out var stableId);
            entries.Add(new EventEntry
            {
                sourceAsset = assetPath,
                targetFileId = fileId,
                targetStableId = stableId ?? "",
                methodName = methodName,
                targetTypeName = typeName
            });
        }
    }
}

class InspectorRefsPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var path in imported)
        {
            if (path.EndsWith(".unity") || path.EndsWith(".prefab"))
            {
                InspectorRefsEmitter.Emit();
                return;
            }
        }
    }
}
