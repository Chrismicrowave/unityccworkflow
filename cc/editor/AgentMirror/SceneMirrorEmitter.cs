using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Emits SceneMirror.json — the canonical entity registry by stableId.
/// Refreshes on hierarchy change, prefab apply, and asset import.
/// Addresses pain #2 (lookup misses), #4 (orientation bloat), #5 (prefab/instance confusion).
/// </summary>
[InitializeOnLoad]
public static class SceneMirrorEmitter
{
    // Debounce: avoid emitting on every single keystroke rename
    private static double _lastEmitTime;
    private const double DebounceSeconds = 0.5;

    static SceneMirrorEmitter()
    {
        EditorApplication.hierarchyChanged        += OnHierarchyChanged;
        PrefabUtility.prefabInstanceUpdated       += OnPrefabInstanceUpdated;
        EditorApplication.delayCall              += EmitNow; // emit once on load
    }

    private static void OnHierarchyChanged()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now - _lastEmitTime < DebounceSeconds) return;
        _lastEmitTime = now;
        EmitNow();
    }

    private static void OnPrefabInstanceUpdated(GameObject _) => EmitNow();

    /// <summary>Called by AssetPostprocessor when .prefab files are imported.</summary>
    public static void OnPrefabImported() => EmitNow();

    /// <summary>Full emit — public so StableIdBootstrap can call it on demand.</summary>
    public static void EmitNow()
    {
        try
        {
            AgentMirrorConfig.EnsureOutputDir();

            var entities = new Dictionary<string, EntityData>();
            int sceneCount = EditorSceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                    WalkGameObject(root, "", scene.path, entities);
            }

            string json = SerializeEntities(entities);
            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.SceneMirrorPath, json);

            // Meta file
            string meta = $"{{\"emittedAt\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\"," +
                          $"\"entityCount\":{entities.Count}," +
                          $"\"sceneCount\":{sceneCount}}}";
            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.SceneMirrorMetaPath, meta);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentMirror] SceneMirrorEmitter failed: {ex.Message}");
        }
    }

    private static void WalkGameObject(GameObject go, string parentPath, string scenePath, Dictionary<string, EntityData> entities)
    {
        string path = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";

        var stableId = go.GetComponent<StableId>();
        if (stableId != null && !string.IsNullOrEmpty(stableId.Id))
        {
            // Determine kind
            string kind = PrefabUtility.IsPartOfPrefabAsset(go) ? "prefab-asset" : "scene-instance";

            // Prefab source GUID
            string prefabSource = "";
            var correspondingObj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
            if (correspondingObj != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(correspondingObj);
                if (!string.IsNullOrEmpty(assetPath))
                    prefabSource = "guid:" + AssetDatabase.AssetPathToGUID(assetPath);
            }

            // Components (exclude Transform to reduce noise)
            var comps = new List<string>();
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue; // missing script
                string typeName = c.GetType().Name;
                if (typeName != "Transform")
                    comps.Add(typeName);
            }

            entities[stableId.Id] = new EntityData
            {
                name        = go.name,
                path        = path,
                kind        = kind,
                prefabSource = prefabSource,
                components  = comps,
                scene       = scenePath
            };
        }

        // Recurse children
        foreach (Transform child in go.transform)
            WalkGameObject(child.gameObject, path, scenePath, entities);
    }

    private static string SerializeEntities(Dictionary<string, EntityData> entities)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        int idx = 0;
        foreach (var kv in entities)
        {
            string comps = "[" + string.Join(",", System.Linq.Enumerable.Select(kv.Value.components, c => $"\"{EscapeJson(c)}\"")) + "]";
            sb.Append($"  \"{EscapeJson(kv.Key)}\": {{");
            sb.Append($"\"name\":\"{EscapeJson(kv.Value.name)}\",");
            sb.Append($"\"path\":\"{EscapeJson(kv.Value.path)}\",");
            sb.Append($"\"kind\":\"{kv.Value.kind}\",");
            sb.Append($"\"prefabSource\":\"{EscapeJson(kv.Value.prefabSource)}\",");
            sb.Append($"\"components\":{comps},");
            sb.Append($"\"scene\":\"{EscapeJson(kv.Value.scene)}\"");
            sb.Append("}");
            if (++idx < entities.Count) sb.Append(",");
            sb.AppendLine();
        }
        sb.Append("}");
        return sb.ToString();
    }

    private static string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private struct EntityData
    {
        public string name;
        public string path;
        public string kind;
        public string prefabSource;
        public List<string> components;
        public string scene;
    }
}

/// <summary>
/// Asset postprocessor that triggers SceneMirrorEmitter when prefab files are imported.
/// </summary>
public class SceneMirrorAssetPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (var path in importedAssets)
        {
            if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                SceneMirrorEmitter.OnPrefabImported();
                return;
            }
        }
    }
}
