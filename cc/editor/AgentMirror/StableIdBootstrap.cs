using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time bootstrap: adds StableId to all selected GameObjects and their children.
/// Run this once when first introducing AgentMirror to an existing project.
/// </summary>
public static class StableIdBootstrap
{
    [MenuItem("Tools/AgentMirror/Add StableId to selection (recursive)")]
    static void AddToSelection()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "AgentMirror",
                "No GameObjects selected.\nSelect one or more root GameObjects in the Hierarchy first.",
                "OK");
            return;
        }

        int added = 0;
        foreach (var go in Selection.gameObjects)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<StableId>() == null)
                {
                    Undo.AddComponent<StableId>(t.gameObject);
                    added++;
                }
            }
        }

        Debug.Log($"[AgentMirror] StableId bootstrap: added {added} component(s) to selection.");
    }

    [MenuItem("Tools/AgentMirror/Add StableId to selection (recursive)", validate = true)]
    static bool ValidateAddToSelection() => Selection.gameObjects.Length > 0;

    [MenuItem("Tools/AgentMirror/Regenerate all mirrors")]
    static void RegenerateAll()
    {
        // Trigger all emitters by simulating a hierarchy change.
        // Each emitter listens to EditorApplication.hierarchyChanged or runs its own full scan.
        SceneMirrorEmitter.EmitNow();
        AsmdefGraphEmitter.EmitNow();
        AnimatorDumpEmitter.EmitNow();
        ProjectDigestEmitter.EmitNow();
        PrefabGraphEmitter.EmitNow();
        InspectorRefsEmitter.EmitNow();
        Debug.Log("[AgentMirror] All mirrors regenerated.");
    }
}
