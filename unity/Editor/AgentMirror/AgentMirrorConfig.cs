using UnityEditor;
using System.IO;

/// <summary>
/// Shared constants for all AgentMirror emitters.
/// Also bootstraps the output directory on domain reload.
/// </summary>
[InitializeOnLoad]
public static class AgentMirrorConfig
{
    // ── Output paths (relative to Unity project root) ──────────────────────
    public const string OutputDir             = "Library/AgentMirror";
    public const string SceneMirrorPath       = "Library/AgentMirror/SceneMirror.json";
    public const string SceneMirrorMetaPath   = "Library/AgentMirror/SceneMirror.meta.json";
    public const string AsmdefGraphPath       = "Library/AgentMirror/AsmdefGraph.json";
    public const string AnimatorDumpPath      = "Library/AgentMirror/AnimatorDump.json";
    public const string SessionLedgerPath     = "Library/AgentMirror/SessionLedger.jsonl";
    public const string ProjectDigestPath     = "Library/AgentMirror/ProjectDigest.md";
    public const string CorrectionLedgerPath  = "Library/AgentMirror/CorrectionLedger.md";
    public const string HookAuditPath         = "Library/AgentMirror/HookAudit.jsonl";
    public const string RefactorEventPath     = "Library/AgentMirror/RefactorEvent.json";
    public const string PrefabGraphPath       = "Library/AgentMirror/PrefabGraph.json";
    public const string InspectorRefsPath     = "Library/AgentMirror/InspectorRefs.json";

    // ── Bootstrap ───────────────────────────────────────────────────────────
    static AgentMirrorConfig()
    {
        EnsureOutputDir();
    }

    /// <summary>Creates Library/AgentMirror/ if it does not exist.</summary>
    public static void EnsureOutputDir()
    {
        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);
    }

    /// <summary>
    /// Safe write helper used by all emitters.
    /// Catches and logs exceptions so a failing emitter never crashes Unity.
    /// </summary>
    public static void SafeWriteAllText(string path, string content)
    {
        try
        {
            EnsureOutputDir();
            File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[AgentMirror] Failed to write {path}: {ex.Message}");
        }
    }
}
