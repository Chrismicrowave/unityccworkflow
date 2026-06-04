using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

/// <summary>
/// Watches for a signal file from the git pre-commit hook.
/// When the signal is detected, saves all open scenes and writes
/// a confirmation so the git hook can proceed.
///
/// Signal file: Library/AgentMirror/.save-scene-signal
/// Done file:   Library/AgentMirror/.save-scene-done
///
/// This avoids calling Unity from command-line (slow) by piggybacking
/// on the already-running Unity Editor instance.
/// </summary>
[InitializeOnLoad]
public static class SceneSaver
{
    private static double _lastCheck;

    static SceneSaver()
    {
        EditorApplication.update += PollSaveSignal;
    }

    private static void PollSaveSignal()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now - _lastCheck < 1.0) return; // once per second is enough
        _lastCheck = now;

        string signalPath = AgentMirrorConfig.SaveSceneSignalPath;
        if (!File.Exists(signalPath)) return;

        // Signal detected — save everything
        Debug.Log("[AgentMirror] Git pre-commit signal detected — saving scenes...");

        try
        {
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.SaveSceneDonePath, "ok");
            Debug.Log("[AgentMirror] Scenes saved successfully for git commit.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[AgentMirror] Scene save failed: {ex.Message}");
            // Write done anyway so git hook doesn't hang
            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.SaveSceneDonePath, $"error:{ex.Message}");
        }
    }

    /// <summary>Manual save trigger. Returns true if Unity responded.</summary>
    [MenuItem("Tools/AgentMirror/Save All Scenes")]
    public static void SaveAllScenes()
    {
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[AgentMirror] All scenes saved manually.");
    }
}
