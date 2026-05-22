# /setupUnityMirror

Create a git worktree mirror for playtesting in a separate directory while Claude works in the main project. The mirror is a full working copy that shares the same git history.

Once set up, use `/updateUnityMirror` to sync the latest changes.

## Steps

1. **Confirm the mirror path** — Propose the default: a sibling directory named `<project-folder>-mirror` (e.g. `Unity-mirror` next to `TechLead`).
   - Ask the user: "Mirror path?" with the default pre-filled (plain text).
   - Allow a custom path.

2. **Check for uncommitted work** — Run `git status --short`. If dirty, ask to commit first before creating the worktree (plain text).

3. **Create the worktree** — Detached HEAD is fine for a playtest mirror:
   ```bash
   git worktree add --detach <mirror-path> HEAD
   ```
   Explain: "Worktree created at `<mirror-path>` (detached HEAD at `<commit>`). You can open this folder in Unity Hub to playtest."

4. **Offer Library symlink** — Ask: "Symlink the Library folder to avoid reimporting everything in Unity?" (plain text). This saves hours of import time.
   - If yes, symlink:
     ```powershell
     New-Item -ItemType SymbolicLink -Path <mirror-path>\Library -Target <project-path>\Library
     ```
   - If the mirror already has a Library folder, delete it first (confirm with user).

5. **Report** — Show:
   - Mirror path
   - Current commit hash and message
   - How to use: "Open `<mirror-path>` in Unity Hub to playtest."
   - How to sync later: "Run `/updateUnityMirror` to pull the latest changes."
