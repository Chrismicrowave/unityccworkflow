using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// UnitySnapshot — FolderSnapshot.
///
/// Recursively scans Assets/ and produces a complete file/folder index at
/// Library/AgentMirror/FolderSnapshot.json.
///
/// Replaces list_files/Glob/search_files MCP calls by giving the AI a
/// complete snapshot of the project's asset tree.
///
/// Triggers: asset import, delete, move, and periodically.
/// </summary>
[InitializeOnLoad]
public static class FolderSnapshotEmitter
{
    [Serializable]
    private class IndexData
    {
        public string emittedAt;
        public int totalFiles;
        public int totalFolders;
        public List<FolderEntry> folders = new List<FolderEntry>();
        public List<FileEntry> files = new List<FileEntry>();
    }

    [Serializable]
    private class FolderEntry
    {
        public string path;
        public string name;
        public int childCount;
    }

    [Serializable]
    private class FileEntry
    {
        public string path;
        public string name;
        public string ext;        // extension including dot, e.g. ".cs"
        public long sizeBytes;
        public string lastModified;
        public string kind;       // "script", "scene", "prefab", "asset", "shader", "image", "audio", "model", "text", "other"
    }

    static FolderSnapshotEmitter()
    {
        EditorApplication.delayCall += () => MarkDirty(5.0); // 5s after launch
        EditorApplication.update += PollEmit;
    }

    private static bool _dirty;
    private static double _lastEmit;
    private static double _debounce = 2.0;

    /// <summary>Request a re-index. Debounce prevents rapid re-scans.</summary>
    public static void MarkDirty(double debounceSeconds = 2.0)
    {
        _dirty = true;
        _debounce = debounceSeconds;
    }

    /// <summary>Call from AssetPostprocessor to trigger re-index on import.</summary>
    public static void OnAssetChanged() => MarkDirty(3.0);

    private static void PollEmit()
    {
        if (!_dirty) return;
        double now = EditorApplication.timeSinceStartup;
        if (now - _lastEmit < _debounce) return;
        _lastEmit = now;
        _dirty = false;
        EmitNow();
    }

    [MenuItem("Tools/AgentMirror/UnitySnapshot/Folder Snapshot")]
    public static void EmitNow()
    {
        try
        {
            AgentMirrorConfig.EnsureOutputDir();
            string assetsPath = Application.dataPath;

            var data = new IndexData
            {
                emittedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            ScanDirectory(assetsPath, "Assets", data);

            data.totalFiles = data.files.Count;
            data.totalFolders = data.folders.Count;

            string json = JsonUtility.ToJson(data, true);
            AgentMirrorConfig.SafeWriteAllText(
                AgentMirrorConfig.FolderSnapshotPath, json);

            // Trim Markdown to a compact summary (full index would be too large for context)
            string summary = $"# Folder Snapshot\n" +
                $"Emitted: {data.emittedAt}\n" +
                $"Files: {data.totalFiles} | Folders: {data.totalFolders}\n\n" +
                $"## By extension\n" +
                string.Join("\n", data.files.GroupBy(f => f.ext)
                    .Select(g => $"- {g.Key}: {g.Count()} files")
                    .OrderByDescending(s => int.TryParse(s.Split(':')[1], out var n) ? n : 0));

            AgentMirrorConfig.SafeWriteAllText(
                AgentMirrorConfig.FolderSnapshotMdPath, summary);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentMirror] FolderSnapshotEmitter failed: {ex.Message}");
        }
    }

    private static readonly HashSet<string> ScriptExts = new() { ".cs", ".asmdef", ".asmref" };
    private static readonly HashSet<string> SceneExts = new() { ".unity" };
    private static readonly HashSet<string> PrefabExts = new() { ".prefab" };
    private static readonly HashSet<string> ShaderExts = new() { ".shader", ".shadergraph", ".hlsl", ".cginc" };
    private static readonly HashSet<string> ImageExts = new() { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tiff", ".exr", ".gif" };
    private static readonly HashSet<string> AudioExts = new() { ".wav", ".mp3", ".ogg", ".aiff", ".aif" };
    private static readonly HashSet<string> ModelExts = new() { ".fbx", ".obj", ".glb", ".gltf", ".blend", ".dae" };
    private static readonly HashSet<string> TextExts = new() { ".md", ".txt", ".json", ".xml", ".yaml", ".yml" };

    private static string ClassifyExtension(string ext)
    {
        if (ScriptExts.Contains(ext)) return "script";
        if (SceneExts.Contains(ext)) return "scene";
        if (PrefabExts.Contains(ext)) return "prefab";
        if (ShaderExts.Contains(ext)) return "shader";
        if (ImageExts.Contains(ext)) return "image";
        if (AudioExts.Contains(ext)) return "audio";
        if (ModelExts.Contains(ext)) return "model";
        if (TextExts.Contains(ext)) return "text";
        return "asset";
    }

    private static void ScanDirectory(string dirPath, string relativeRoot, IndexData data)
    {
        var dirInfo = new DirectoryInfo(dirPath);
        if (!dirInfo.Exists) return;

        // Skip hidden folders (starting with .)
        if (dirInfo.Name.StartsWith(".")) return;
        // Skip common non-asset folders
        if (dirInfo.Name == "Library" || dirInfo.Name == "obj") return;

        // Add folder entry
        string relPath = "Assets" + dirPath.Substring(Application.dataPath.Length);
        var childFiles = dirInfo.GetFiles().Where(f => !f.Name.StartsWith(".") && !f.Name.EndsWith(".meta")).ToList();
        data.folders.Add(new FolderEntry
        {
            path = relPath,
            name = dirInfo.Name,
            childCount = childFiles.Count
        });

        // Add file entries
        foreach (var file in childFiles)
        {
            string fileRelPath = relPath + "/" + file.Name;
            string ext = file.Extension.ToLowerInvariant();

            data.files.Add(new FileEntry
            {
                path = fileRelPath,
                name = file.Name,
                ext = ext,
                sizeBytes = file.Length,
                lastModified = file.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                kind = ClassifyExtension(ext)
            });
        }

        // Recurse into subdirectories
        foreach (var subDir in dirInfo.GetDirectories().Where(d => !d.Name.StartsWith(".")))
            ScanDirectory(subDir.FullName, relativeRoot, data);
    }
}

/// <summary>
/// Triggers project index refresh whenever assets change.
/// </summary>
public class ProjectIndexPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (importedAssets.Length > 0 || deletedAssets.Length > 0 || movedAssets.Length > 0)
            FolderSnapshotEmitter.OnAssetChanged();
    }
}
