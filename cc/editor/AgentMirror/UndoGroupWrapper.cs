using UnityEditor;

/// <summary>
/// Utility for wrapping agent-driven edits in named Undo groups.
/// Call Begin() before any set of mutations, End() after — the user
/// can undo the entire batch with a single Ctrl+Z.
///
/// Usage from execute_script:
///   UndoGroupWrapper.Begin("AgentMirror: rename patrol waypoints");
///   // ...mutations...
///   int groupId = UndoGroupWrapper.End();
/// </summary>
public static class UndoGroupWrapper
{
    /// <summary>
    /// Opens a new Undo group with the given description.
    /// All subsequent Undo.RecordObject / Undo.AddComponent calls
    /// until End() will collapse into this group.
    /// </summary>
    public static void Begin(string description)
    {
        Undo.SetCurrentGroupName(description);
        Undo.IncrementCurrentGroup();
    }

    /// <summary>
    /// Closes the current Undo group.
    /// Returns the group index — store it if you need to call
    /// Undo.CollapseUndoOperations(groupId) to force the merge.
    /// </summary>
    public static int End() => Undo.GetCurrentGroup();
}
