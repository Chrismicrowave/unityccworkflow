using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Emits AsmdefGraph.json — assembly dependency graph and file→assembly map.
/// Addresses pain #1 (compile churn): most CS0246 errors are asmdef misses.
/// </summary>
[InitializeOnLoad]
public static class AsmdefGraphEmitter
{
    static AsmdefGraphEmitter()
    {
        EditorApplication.delayCall += EmitNow;
    }

    /// <summary>Full emit — public so StableIdBootstrap can trigger on demand.</summary>
    public static void EmitNow()
    {
        try
        {
            AgentMirrorConfig.EnsureOutputDir();

            var assemblies = new Dictionary<string, AsmdefData>();

            string[] guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                string json = File.ReadAllText(assetPath);
                AsmdefRaw raw = JsonUtility.FromJson<AsmdefRaw>(json);
                if (raw == null || string.IsNullOrEmpty(raw.name)) continue;

                // Root directory of this asmdef
                string rootDir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? "";

                // Find all .cs files under this asmdef's directory
                string[] csGuids = AssetDatabase.FindAssets("t:Script", new[] { rootDir });
                var files = new List<string>();
                foreach (var csGuid in csGuids)
                {
                    string csPath = AssetDatabase.GUIDToAssetPath(csGuid);
                    if (csPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        files.Add(csPath);
                }

                // Determine if editor-only
                bool editorOnly = false;
                if (raw.includePlatforms != null)
                {
                    foreach (var p in raw.includePlatforms)
                        if (p == "Editor") { editorOnly = true; break; }
                }

                // Clean reference names: strip leading "GUID:" prefix if present
                var cleanRefs = new List<string>();
                if (raw.references != null)
                {
                    foreach (var r in raw.references)
                    {
                        if (r.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
                        {
                            // Resolve GUID to assembly name
                            string refGuid = r.Substring(5);
                            string refPath = AssetDatabase.GUIDToAssetPath(refGuid);
                            if (!string.IsNullOrEmpty(refPath))
                            {
                                string refJson = File.ReadAllText(refPath);
                                AsmdefRaw refRaw = JsonUtility.FromJson<AsmdefRaw>(refJson);
                                if (refRaw != null && !string.IsNullOrEmpty(refRaw.name))
                                {
                                    cleanRefs.Add(refRaw.name);
                                    continue;
                                }
                            }
                        }
                        cleanRefs.Add(r);
                    }
                }

                assemblies[raw.name] = new AsmdefData
                {
                    rootDir    = rootDir,
                    files      = files,
                    references = cleanRefs,
                    editorOnly = editorOnly
                };
            }

            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.AsmdefGraphPath, SerializeGraph(assemblies));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentMirror] AsmdefGraphEmitter failed: {ex.Message}");
        }
    }

    private static string SerializeGraph(Dictionary<string, AsmdefData> assemblies)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        int idx = 0;
        foreach (var kv in assemblies)
        {
            string filesArr = "[" + string.Join(",",
                System.Linq.Enumerable.Select(kv.Value.files, f => $"\"{EscJ(f)}\"")) + "]";
            string refsArr = "[" + string.Join(",",
                System.Linq.Enumerable.Select(kv.Value.references, r => $"\"{EscJ(r)}\"")) + "]";

            sb.Append($"  \"{EscJ(kv.Key)}\": {{");
            sb.Append($"\"rootDir\":\"{EscJ(kv.Value.rootDir)}\",");
            sb.Append($"\"files\":{filesArr},");
            sb.Append($"\"references\":{refsArr},");
            sb.Append($"\"editorOnly\":{(kv.Value.editorOnly ? "true" : "false")}");
            sb.Append("}");
            if (++idx < assemblies.Count) sb.Append(",");
            sb.AppendLine();
        }
        sb.Append("}");
        return sb.ToString();
    }

    private static string EscJ(string s) =>
        s == null ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    // ── Minimal deserialization types for asmdef JSON ──────────────────────
    [Serializable]
    private class AsmdefRaw
    {
        public string   name;
        public string[] references;
        public string[] includePlatforms;
    }

    private struct AsmdefData
    {
        public string       rootDir;
        public List<string> files;
        public List<string> references;
        public bool         editorOnly;
    }
}

/// <summary>Triggers AsmdefGraphEmitter when .asmdef files are imported.</summary>
public class AsmdefAssetPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (var path in importedAssets)
        {
            if (path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
            {
                AsmdefGraphEmitter.EmitNow();
                return;
            }
        }
    }
}
