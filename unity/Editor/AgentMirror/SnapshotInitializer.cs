using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Watches for Library/AgentMirror/.generate-snapshots-signal (written by
/// session-start hook) and triggers the UnitySnapshot generation once.
/// This is how the snapshot system bootstraps itself on first Claude session.
/// </summary>
[InitializeOnLoad]
public static class SnapshotInitializer
{
    private static double _lastCheck;

    static SnapshotInitializer()
    {
        EditorApplication.update += PollSignal;
    }

    private static void PollSignal()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now - _lastCheck < 2.0) return;
        _lastCheck = now;

        string signal = "Library/AgentMirror/.generate-snapshots-signal";
        if (!File.Exists(signal)) return;

        File.Delete(signal);

        // Trigger all snapshot generators via menu item
        Debug.Log("[AgentMirror] Triggering initial UnitySnapshot generation...");
        EditorApplication.ExecuteMenuItem("Tools/AgentMirror/UnitySnapshot/Generate All");
    }
}
