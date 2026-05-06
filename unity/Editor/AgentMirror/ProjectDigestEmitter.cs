using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Emits ProjectDigest.md on every Unity editor launch.
/// Content: recent git commits, changed files, uncommitted state, last session summary.
/// Addresses codebase drift across sessions and post-compaction state loss.
/// </summary>
[InitializeOnLoad]
public static class ProjectDigestEmitter
{
    static ProjectDigestEmitter()
    {
        // Delay to let other InitializeOnLoad scripts settle
        EditorApplication.delayCall += EmitNow;
    }

    /// <summary>Full emit — public so StableIdBootstrap can trigger on demand.</summary>
    public static void EmitNow()
    {
        try
        {
            AgentMirrorConfig.EnsureOutputDir();

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            var sb = new StringBuilder();
            sb.AppendLine($"# Project Digest — {timestamp}");
            sb.AppendLine();

            // ── Git: recent commits ────────────────────────────────────────
            sb.AppendLine("## Recent commits");
            string gitLog = RunGit("log --oneline -5");
            sb.AppendLine(string.IsNullOrWhiteSpace(gitLog) ? "_git not available_" : gitLog);
            sb.AppendLine();

            // ── Git: files changed in last commit ──────────────────────────
            sb.AppendLine("## Files changed (last commit)");
            string gitDiff = RunGit("diff --name-only HEAD~1");
            sb.AppendLine(string.IsNullOrWhiteSpace(gitDiff) ? "_none or first commit_" : gitDiff);
            sb.AppendLine();

            // ── Git: uncommitted state ─────────────────────────────────────
            sb.AppendLine("## Uncommitted");
            string gitStatus = RunGit("status --short");
            sb.AppendLine(string.IsNullOrWhiteSpace(gitStatus) ? "_clean_" : gitStatus);
            sb.AppendLine();

            // ── Last session summary from SessionLedger ────────────────────
            sb.AppendLine("## Last session");
            if (File.Exists(AgentMirrorConfig.SessionLedgerPath))
            {
                var lines = new System.Collections.Generic.List<string>();
                // Read last 10 lines efficiently without loading the whole file
                using (var stream = new FileStream(AgentMirrorConfig.SessionLedgerPath, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    var buffer = new System.Collections.Generic.Queue<string>(10);
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (buffer.Count == 10) buffer.Dequeue();
                        buffer.Enqueue(line);
                    }
                    lines.AddRange(buffer);
                }
                if (lines.Count > 0)
                    sb.AppendLine(string.Join("\n", lines));
                else
                    sb.AppendLine("_No session ledger entries yet._");
            }
            else
            {
                sb.AppendLine("_SessionLedger.jsonl not found — no prior session recorded._");
            }

            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.ProjectDigestPath, sb.ToString());
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[AgentMirror] ProjectDigestEmitter failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs a git command in the Unity project root and returns stdout.
    /// Returns empty string (not an exception) if git is unavailable or fails.
    /// </summary>
    private static string RunGit(string args)
    {
        try
        {
            // Unity's Application.dataPath is Assets/ — project root is one level up
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory       = projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            using var process = Process.Start(psi);
            if (process == null) return "_git process failed to start_";

            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000); // 5 second timeout

            return output;
        }
        catch
        {
            return "_git not available_";
        }
    }
}
