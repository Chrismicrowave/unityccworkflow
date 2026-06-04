using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Scans all GameObjects with StableId in open scenes and prefab assets,
/// then produces a component wiring report at Library/AgentMirror/ComponentRegistry.json
/// and a human-readable markdown at Library/AgentMirror/ComponentRegistry.md.
///
/// The report captures:
///   - Each GameObject's components
///   - Serialized object references (who references who, by StableId)
///   - Key field values (primitives that drive behavior)
///
/// Hook trigger: auto-regenerates on scene save, prefab import, and component change.
/// Session start: hook loads the latest registry into Claude's context.
/// </summary>
[InitializeOnLoad]
public static class ComponentRegistryGenerator
{
    // ── Data model ──────────────────────────────────────────────────────

    [Serializable]
    private class RegistryData
    {
        public string emittedAt;
        public int entityCount;
        public int referenceCount;
        public List<EntityEntry> entities = new List<EntityEntry>();
    }

    [Serializable]
    private class EntityEntry
    {
        public string stableId;      // null if GameObject has no StableId component
        public string name;
        public string path;
        public int componentCount;   // total components on this GameObject
        public string kind;          // "scene-instance" or "prefab-asset"
        public string scene;         // scene path or prefab asset path
        public List<ComponentEntry> components = new List<ComponentEntry>();
    }

    [Serializable]
    private class ComponentEntry
    {
        public string type;
        public List<FieldEntry> fields = new List<FieldEntry>();
    }

    [Serializable]
    private class FieldEntry
    {
        public string name;
        public string valueType;  // "primitive", "gameObjectRef", "componentRef", "assetRef"
        public string value;      // display-friendly value
        public string targetStableId;  // populated for GameObject/Component refs
    }

    // ── Lifecycle ───────────────────────────────────────────────────────

    static ComponentRegistryGenerator()
    {
        EditorApplication.hierarchyChanged += MarkDirty;
        EditorApplication.playModeStateChanged += _ => MarkDirty();
        Undo.postprocessModifications += OnUndoRedo;
        // Delay initial emit so all emitters load first
        EditorApplication.delayCall += () =>
        {
            _dirty = true;
        };
        // Periodic background emit (every 30s if dirty)
        EditorApplication.update += PollEmit;
    }

    private static bool _dirty;
    private static double _lastEmit;

    private static UndoPropertyModification[] OnUndoRedo(UndoPropertyModification[] modifications)
    {
        MarkDirty();
        return modifications;
    }

    private static void MarkDirty() => _dirty = true;

    private static void PollEmit()
    {
        if (!_dirty) return;
        double now = EditorApplication.timeSinceStartup;
        if (now - _lastEmit < 2.0) return; // 2s debounce for quick serial edits
        _lastEmit = now;
        _dirty = false;
        EmitNow();
    }

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>Call from menu or hook to force regeneration.</summary>
    [MenuItem("Tools/AgentMirror/Generate Component Registry")]
    public static void EmitNow()
    {
        try
        {
            AgentMirrorConfig.EnsureOutputDir();

            var data = new RegistryData
            {
                emittedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            // ── Scan open scenes ──────────────────────────────────────────
            var scannedGo = new HashSet<GameObject>();
            int sceneCount = EditorSceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    WalkGameObject(root, "", scene.path, "scene-instance", data, scannedGo);
            }

            // ── Scan prefabs ──────────────────────────────────────────────
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (var guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                WalkGameObject(prefab, "", path, "prefab-asset", data, scannedGo);
            }

            data.entityCount = data.entities.Count;
            data.referenceCount = 0;
            foreach (var e in data.entities)
                foreach (var c in e.components)
                    data.referenceCount += c.fields.Count;

            // ── Write JSON ────────────────────────────────────────────────
            string json = JsonUtility.ToJson(data, true);
            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.ComponentRegistryPath, json);

            // ── Write Markdown ────────────────────────────────────────────
            string md = RenderMarkdown(data);
            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.ComponentRegistryMdPath, md);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentMirror] ComponentRegistryGenerator failed: {ex.Message}");
        }
    }

    // ── Walk ────────────────────────────────────────────────────────────

    private static void WalkGameObject(
        GameObject go, string parentPath, string sceneOrPrefabPath,
        string kind, RegistryData data, HashSet<GameObject> scanned)
    {
        if (scanned.Contains(go)) return;
        scanned.Add(go);

        string path = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";
        var stableId = go.GetComponent<StableId>();
        string stableIdStr = stableId != null ? stableId.Id : null;

        // Walk children first
        foreach (Transform child in go.transform)
            WalkGameObject(child.gameObject, path, sceneOrPrefabPath, kind, data, scanned);

        // Capture every GameObject — no StableId gate.
        var entry = new EntityEntry
        {
            stableId = stableIdStr,
            name = go.name,
            path = path,
            componentCount = 0,
            kind = kind,
            scene = sceneOrPrefabPath
        };

        var allComponents = go.GetComponents<Component>();
        entry.componentCount = allComponents.Length;

        foreach (var comp in allComponents)
        {
            if (comp == null || comp is Transform) continue;

            var compEntry = new ComponentEntry { type = comp.GetType().Name };
            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.name == "m_Script") continue; // always a script reference, noise
                // Use the full property path (handles nested structs like OutputChannel.Index)
                // Strip the root component prefix to keep it readable
                string fieldName = prop.propertyPath;
                var field = CaptureField(prop);
                if (field != null)
                {
                    field.name = fieldName; // override with full path
                    compEntry.fields.Add(field);
                }
            }

            if (compEntry.fields.Count > 0 || ShouldShowEmptyComponent(comp))
                entry.components.Add(compEntry);
        }

        data.entities.Add(entry);
    }

    private static bool ShouldShowEmptyComponent(Component comp)
    {
        // Always show script components even without visible fields
        // (e.g., empty MonoBehaviour used as marker)
        return comp is MonoBehaviour;
    }

    private static FieldEntry CaptureField(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.ObjectReference when prop.objectReferenceValue != null:
                return CaptureObjectRef(prop);

            case SerializedPropertyType.Boolean:
                return new FieldEntry
                {
                    name = prop.name,
                    valueType = "primitive",
                    value = prop.boolValue.ToString()
                };

            case SerializedPropertyType.Float:
                return new FieldEntry
                {
                    name = prop.name,
                    valueType = "primitive",
                    value = prop.floatValue.ToString("G4")
                };

            case SerializedPropertyType.Integer:
                return new FieldEntry
                {
                    name = prop.name,
                    valueType = "primitive",
                    value = prop.intValue.ToString()
                };

            case SerializedPropertyType.String:
                return new FieldEntry
                {
                    name = prop.name,
                    valueType = "primitive",
                    value = string.IsNullOrEmpty(prop.stringValue)
                        ? "(empty)" : Truncate(prop.stringValue, 80)
                };

            case SerializedPropertyType.Enum:
                return new FieldEntry
                {
                    name = prop.name,
                    valueType = "primitive",
                    value = prop.enumNames[prop.enumValueIndex]
                };

            case SerializedPropertyType.Color:
                var c = prop.colorValue;
                return new FieldEntry
                {
                    name = prop.name,
                    valueType = "primitive",
                    value = $"rgba({c.r:F2},{c.g:F2},{c.b:F2},{c.a:F2})"
                };

            case SerializedPropertyType.Vector2:
                var v2 = prop.vector2Value;
                return new FieldEntry
                {
                    name = prop.name,
                    valueType = "primitive",
                    value = $"({v2.x:G4}, {v2.y:G4})"
                };

            case SerializedPropertyType.Vector3:
                var v3 = prop.vector3Value;
                return new FieldEntry
                {
                    name = prop.name,
                    valueType = "primitive",
                    value = $"({v3.x:G4}, {v3.y:G4}, {v3.z:G4})"
                };

            default:
                return null; // skip arrays, gradients, animation curves etc.
        }
    }

    private static FieldEntry CaptureObjectRef(SerializedProperty prop)
    {
        var target = prop.objectReferenceValue;
        string targetStableId = null;

        // Resolve GameObject reference to StableId
        GameObject targetGo = null;
        if (target is GameObject go) targetGo = go;
        else if (target is Component comp) targetGo = comp.gameObject;

        if (targetGo != null)
        {
            var sid = targetGo.GetComponent<StableId>();
            if (sid != null && !string.IsNullOrEmpty(sid.Id))
                targetStableId = sid.Id;
        }

        string valueType = "assetRef";
        string value = target.name;

        if (target is GameObject)
        {
            valueType = targetStableId != null ? "gameObjectRef" : "assetRef";
        }
        else if (target is Component)
        {
            valueType = targetStableId != null ? "componentRef" : "assetRef";
            value = $"{target.GetType().Name} on {targetGo?.name ?? "(unknown)"}";
        }
        else if (target is Material || target is Texture || target is Mesh ||
                 target is AudioClip || target is AnimationClip)
        {
            valueType = "assetRef";
            string assetPath = AssetDatabase.GetAssetPath(target);
            if (!string.IsNullOrEmpty(assetPath))
                value = assetPath;
        }

        return new FieldEntry
        {
            name = prop.name,
            valueType = valueType,
            value = value,
            targetStableId = targetStableId
        };
    }

    // ── Render ──────────────────────────────────────────────────────────

    private static string RenderMarkdown(RegistryData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Component Registry");
        sb.AppendLine($"Emitted: {data.emittedAt}");
        sb.AppendLine($"Entities: {data.entityCount} | References: {data.referenceCount}");
        sb.AppendLine();

        foreach (var entity in data.entities)
        {
            string sceneLabel = entity.kind == "prefab-asset"
                ? $"📦 {ShortPath(entity.scene)}"
                : $"🎬 {ShortPath(entity.scene)}";

            sb.AppendLine($"## {entity.name} `{ShortStableId(entity.stableId)}` ({sceneLabel})");
            sb.AppendLine($"  Path: `{entity.path}`");

            if (entity.components.Count == 0)
            {
                sb.AppendLine("  *(no components with tracked data)*");
                sb.AppendLine();
                continue;
            }

            foreach (var comp in entity.components)
            {
                sb.AppendLine($"  - **{comp.type}**");
                foreach (var field in comp.fields)
                {
                    string icon = field.valueType switch
                    {
                        "gameObjectRef" => "🔗",
                        "componentRef"  => "🔗",
                        "assetRef"      => "📎",
                        _               => "  "
                    };

                    string targetInfo = field.targetStableId != null
                        ? $" → `{ShortStableId(field.targetStableId)}`"
                        : "";

                    sb.AppendLine($"    {icon} `{field.name}` = {field.value}{targetInfo}");
                }
            }
            sb.AppendLine();
        }

        // ── Cross-reference index ────────────────────────────────────────
        sb.AppendLine("---");
        sb.AppendLine("# Reference Index (who references who)");
        sb.AppendLine();

        foreach (var entity in data.entities)
        {
            var outgoing = new List<ReferenceEdge>();
            foreach (var comp in entity.components)
            {
                foreach (var field in comp.fields)
                {
                    if (field.targetStableId != null &&
                        (field.valueType == "gameObjectRef" || field.valueType == "componentRef"))
                    {
                        outgoing.Add(new ReferenceEdge
                        {
                            fromStableId = entity.stableId,
                            fromComponent = comp.type,
                            field = field.name,
                            toStableId = field.targetStableId
                        });
                    }
                }
            }

            if (outgoing.Count == 0) continue;

            sb.AppendLine($"### {entity.name} `{ShortStableId(entity.stableId)}` references:");
            foreach (var edge in outgoing)
            {
                string targetName = ResolveName(data, edge.toStableId);
                sb.AppendLine($"  - `{edge.fromComponent}.{edge.field}` → {targetName} `{ShortStableId(edge.toStableId)}`");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private class ReferenceEdge
    {
        public string fromStableId;
        public string fromComponent;
        public string field;
        public string toStableId;
    }

    private static string ShortStableId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length < 8) return id ?? "";
        return id.Substring(0, 8) + "…";
    }

    private static string ShortPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        int idx = path.LastIndexOf('/');
        return idx >= 0 ? path.Substring(idx + 1) : path;
    }

    private static string ResolveName(RegistryData data, string stableId)
    {
        foreach (var e in data.entities)
            if (e.stableId == stableId) return e.name;
        return "(unknown)";
    }

    private static string Truncate(string s, int maxLen)
    {
        return s.Length <= maxLen ? s : s.Substring(0, maxLen) + "…";
    }
}
