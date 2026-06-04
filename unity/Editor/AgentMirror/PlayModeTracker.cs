using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Writes Library/AgentMirror/PlayModeState.json whenever play mode changes.
/// Enables the pre-tool-use hook to hard-block writes during play mode
/// while still allowing read-only queries (state inspection, screenshots).
/// </summary>
[InitializeOnLoad]
public static class PlayModeTracker
{
    private static double _lastWrite;

    static PlayModeTracker()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        // Also poll during play mode in case the user toggles via keyboard shortcut
        EditorApplication.update += PollPlayMode;
        WriteState();
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        WriteState();
    }

    private static void PollPlayMode()
    {
        if (!EditorApplication.isPlaying) return;
        // Throttle: once per second during play is sufficient
        double now = EditorApplication.timeSinceStartup;
        if (now - _lastWrite < 1.0) return;
        _lastWrite = now;
        WriteState();
    }

    private static void WriteState()
    {
        try
        {
            AgentMirrorConfig.EnsureOutputDir();
            string json = $"{{\"isPlaying\":{EditorApplication.isPlaying.ToString().ToLower()}}}";
            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.PlayModeStatePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[AgentMirror] PlayModeTracker failed: {ex.Message}");
        }
    }

    /// <summary>Convenience for hooks to read the current state file.</summary>
    public static bool IsPlaying()
    {
        string path = AgentMirrorConfig.PlayModeStatePath;
        if (!File.Exists(path)) return false;
        try
        {
            string json = File.ReadAllText(path);
            return json.Contains("\"isPlaying\":true");
        }
        catch { return false; }
    }
}
