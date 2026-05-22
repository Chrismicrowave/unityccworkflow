# /updateUnityMirror

Update a git worktree mirror with the latest commit from this project. Uses `git worktree list` to find user-created worktrees (excluding `.claude/worktrees/` internal worktrees).

## Steps

1. **Check for uncommitted work** — Run `git status --short`.
   - If dirty, ask: "There are uncommitted changes. Commit them first?" (plain text).
   - If yes, create a commit with a reasonable message.
   - If no, proceed (uncommitted changes won't go to the mirror).

2. **Find the mirror** — Run `git worktree list` and filter for entries whose path does NOT contain `.claude/worktrees/` and is NOT the main working tree. Those are user-created mirrors.
   - If exactly one match, use it.
   - If multiple, list them numbered and ask the user which to update.
   - If none, suggest running `/setupUnityMirror` first.

3. **Fetch and update** — Get the current branch name:
   ```bash
   git branch --show-current
   ```
   Then:
   ```bash
   git fetch origin <current-branch>
   git -C <mirror-path> fetch origin
   git -C <mirror-path> checkout <current-branch>
   git -C <mirror-path> pull origin <current-branch>
   ```

4. **Report** — Show the mirror's new HEAD commit and message.
