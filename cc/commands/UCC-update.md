# UCC-update

Sync this project with the latest UCCPack. Pulls upstream, captures any local project improvements back to the pack, resolves divergences, and syncs everything.

## Steps

1. **Auto-accept** — The user invoked this, proceed without confirmation.

2. **Pull latest pack from GitHub** — In `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\`:
   - `git checkout master && git pull origin master`
   - If pull fails (network/auth), report and STOP.

3. **Check divergence** — For each synced path pair, check:
   - **Uncommitted changes** — `git status --short` on project synced paths
   - **Content divergence** — `git diff --no-index` project ↔ pack
   - **Ahead/behind** — `git log --oneline HEAD..origin/master && git log --oneline origin/master..HEAD` in pack dir

   Synced paths:
   - `.claude/hooks/` ↔ `cc/hooks/`
   - `.claude/skills/unity-mcp-discipline/` ↔ `cc/skills/unity-mcp-discipline/`
   - `.claude/commands/UCC-*.md` ↔ `cc/commands/UCC-*.md`
   - `Assets/Editor/AgentMirror/` ↔ `unity/Editor/AgentMirror/`
   - `Assets/Scripts/Core/StableId.cs` ↔ `unity/Runtime/StableId.cs`

4. **Auto-resolve** — Handle each state:

   **a. Uncommitted project changes** → commit them in the project (auto-generate message describing what changed).

   **b. Content divergence** → copy changed files **project → pack** (project is the source of truth for its own improvements). Commit in pack dir.

   **c. Pack ahead of origin** → `git push origin master` in pack dir.

   **d. Pack behind origin** → already handled by step 2 pull.

5. **Pull again** — After capturing+committing+ pushing, pull again in case the push changed origin: `git pull origin master`.

6. **Sync to project** — Copy pack files into project:
   - `cc/hooks/` → `.claude/hooks/`
   - `cc/skills/unity-mcp-discipline/` → `.claude/skills/unity-mcp-discipline/`
   - `cc/commands/` → `.claude/commands/`
   - `unity/Editor/AgentMirror/` → `Assets/Editor/AgentMirror/`
   - `unity/Runtime/StableId.cs` → `Assets/Scripts/Core/StableId.cs`
   - Preserve existing project files that don't exist in pack.

7. **Update project version** — Read pack version from `pack-info.json`. Write `.claude/pack-version.json` with version, sourceCommit, installedAt.

8. **Report** — Show:
   - What was captured from project → pack
   - What was synced from pack → project
   - Final versions (pack + project)
   - Prompt to commit the project changes
