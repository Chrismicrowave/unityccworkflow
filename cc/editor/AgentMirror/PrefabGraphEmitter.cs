using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

[InitializeOnLoad]
public static class PrefabGraphEmitter
{
    static PrefabGraphEmitter()
    {
        Emit();
    }

    class PrefabEntry
    {
        public int instanceCount;
        public string variantOf;
        public List<string> scenesUsedIn = new List<string>();
        public string assetPath;
        public string prefabType;
    }

    public static void EmitNow() => Emit();

    public static void Emit()
    {
        Directory.CreateDirectory(AgentMirrorConfig.OutputDir);

        var guids = AssetDatabase.FindAssets("t:Prefab");
        var graph = new Dictionary<string, PrefabEntry>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            var entry = new PrefabEntry { assetPath = path };

            var prefabType = PrefabUtility.GetPrefabAssetType(go);
            entry.prefabType = prefabType.ToString();

            if (prefabType == PrefabAssetType.Variant)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
                if (source != null)
                {
                    var sourcePath = AssetDatabase.GetAssetPath(source);
                    var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
                    entry.variantOf = sourceGuid;
                }
            }

            graph[guid] = entry;
        }

        // Count scene instances
        var sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (var sceneGuid in sceneGuids)
        {
            var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            var deps = AssetDatabase.GetDependencies(scenePath, false);
            foreach (var dep in deps)
            {
                var depGuid = AssetDatabase.AssetPathToGUID(dep);
                if (graph.ContainsKey(depGuid))
                {
                    graph[depGuid].instanceCount++;
                    if (!graph[depGuid].scenesUsedIn.Contains(scenePath))
                        graph[depGuid].scenesUsedIn.Add(scenePath);
                }
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("{");
        bool first = true;
        foreach (var kvp in graph)
        {
            if (!first) sb.AppendLine(",");
            first = false;
            var e = kvp.Value;
            sb.Append($"  \"{kvp.Key}\": {{");
            sb.Append($"\"assetPath\": \"{e.assetPath.Replace("\\", "/")}\",");
            sb.Append($"\"prefabType\": \"{e.prefabType}\",");
            sb.Append($"\"instanceCount\": {e.instanceCount},");
            sb.Append($"\"variantOf\": {(e.variantOf != null ? $"\"{e.variantOf}\"" : "null")},");
            sb.Append("\"scenesUsedIn\": [");
            sb.Append(string.Join(",", e.scenesUsedIn.ConvertAll(s => $"\"{s.Replace("\\", "/")}\"").ToArray()));
            sb.Append("]}");
        }
        sb.AppendLine();
        sb.AppendLine("}");

        File.WriteAllText(AgentMirrorConfig.PrefabGraphPath, sb.ToString());
    }
}

class PrefabGraphPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var path in imported)
        {
            if (path.EndsWith(".prefab"))
            {
                PrefabGraphEmitter.Emit();
                return;
            }
        }
    }
}
