using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Emits RefactorEvent.json when a mass-rename/reparent event is detected.
/// Threshold: more than 10 entities change identity in a single import cycle.
///
/// Motivation: The Apr 28 RefactorSceneWiring.cs event (49 components added in one batch)
/// was a watershed — the next session's agent had no warning that its scene model was invalid.
/// This emitter surfaces that so session-start can inject a warning.
/// </summary>
public class RefactorEventEmitter : AssetPostprocessor
{
    // How many entity changes constitute a "mass refactor" event
    private const int MassRefactorThreshold = 10;

    // Snapshot of SceneMirror entity IDs from the last known-good state
    private static HashSet<string> _previousIds = new HashSet<string>();
    private static bool _snapshotTaken = false;

    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        // Only care about scene or prefab changes
        bool sceneOrPrefabChanged = false;
        foreach (var a in importedAssets)
        {
            if (a.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                a.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                sceneOrPrefabChanged = true;
                break;
            }
        }
        if (!sceneOrPrefabChanged) return;

        // Load the current SceneMirror to compare
        string mirrorPath = AgentMirrorConfig.SceneMirrorPath;
        if (!File.Exists(mirrorPath)) return;

        var currentIds = ExtractIdsFromMirror(mirrorPath);

        if (!_snapshotTaken)
        {
            // First run: establish baseline
            _previousIds = currentIds;
            _snapshotTaken = true;
            return;
        }

        // Compute delta
        var added   = new List<string>();
        var removed = new List<string>();

        foreach (var id in currentIds)
            if (!_previousIds.Contains(id)) added.Add(id);
        foreach (var id in _previousIds)
            if (!currentIds.Contains(id)) removed.Add(id);

        int changeCount = added.Count + removed.Count;

        if (changeCount >= MassRefactorThreshold)
        {
            EmitRefactorEvent(changeCount, added, removed);
        }

        // Update snapshot
        _previousIds = currentIds;
    }

    private static HashSet<string> ExtractIdsFromMirror(string path)
    {
        var ids = new HashSet<string>();
        try
        {
            // Parse just the top-level keys from the JSON without a full deserializer.
            // Format: { "id1": {...}, "id2": {...} }
            string json = File.ReadAllText(path);
            int i = 0;
            // Skip opening brace
            while (i < json.Length && json[i] != '{') i++;
            i++;

            while (i < json.Length)
            {
                // Find opening quote of key
                while (i < json.Length && json[i] != '"' && json[i] != '}') i++;
                if (i >= json.Length || json[i] == '}') break;
                i++; // skip "
                int start = i;
                while (i < json.Length && json[i] != '"') i++;
                string key = json.Substring(start, i - start);
                ids.Add(key);
                i++; // skip closing "
                // Skip to next key by finding the next ":" and then the matching object close "}"
                int depth = 0;
                while (i < json.Length)
                {
                    if (json[i] == '{') depth++;
                    else if (json[i] == '}') { if (depth == 0) break; depth--; }
                    i++;
                }
                i++; // skip }
                // Skip comma
                while (i < json.Length && (json[i] == ',' || json[i] == '\n' || json[i] == '\r' || json[i] == ' ')) i++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentMirror] RefactorEventEmitter failed to parse mirror: {ex.Message}");
        }
        return ids;
    }

    private static void EmitRefactorEvent(int count, List<string> added, List<string> removed)
    {
        try
        {
            AgentMirrorConfig.EnsureOutputDir();

            string ts = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Build affectedIds array (cap at 50 to keep file readable)
            var affected = new List<string>(added);
            affected.AddRange(removed);
            if (affected.Count > 50) affected = affected.GetRange(0, 50);

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"ts\":\"{ts}\",");
            sb.Append($"\"type\":\"mass-refactor\",");
            sb.Append($"\"count\":{count},");
            sb.Append($"\"added\":{added.Count},");
            sb.Append($"\"removed\":{removed.Count},");
            sb.Append("\"affectedIds\":[");
            for (int i = 0; i < affected.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"\"{EscJ(affected[i])}\"");
            }
            sb.Append("],");
            sb.Append("\"trigger\":\"AssetPostprocessor batch import\"");
            sb.Append("}");

            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.RefactorEventPath, sb.ToString());
            Debug.LogWarning($"[AgentMirror] Mass refactor detected: {count} entity changes. RefactorEvent.json emitted.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentMirror] RefactorEventEmitter write failed: {ex.Message}");
        }
    }

    private static string EscJ(string s) =>
        s == null ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
