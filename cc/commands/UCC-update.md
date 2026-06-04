# UCC-update

Update the unityccworkflow pack files in this project to the latest version — pulls the pack from GitHub first, then syncs files.

## Steps

1. **Auto-accept: always confirm upgrade** — The user invoked this skill, so proceed without asking "are you sure?".

2. **Capture project changes back to pack** — Before pulling fresh pack files, check if any synced files in the project have diverged from the local pack. This ensures project improvements are saved back before being overwritten.

   For each synced path pair, check for divergences using `git diff --no-index`:
   - `.claude/hooks/` ↔ `cc/hooks/`
   - `.claude/skills/unity-mcp-discipline/` ↔ `cc/skills/unity-mcp-discipline/`
   - `.claude/commands/UCC-*.md` ↔ `cc/commands/UCC-*.md`
   - `Assets/Editor/AgentMirror/` ↔ `unity/Editor/AgentMirror/`
   - `Assets/Scripts/Core/StableId.cs` ↔ `unity/Runtime/StableId.cs`

   If any files differ:
   - Copy changed files **from project → pack** (preserving project improvements)
   - Commit in pack dir: `git add -A && git commit -m "capture: sync from $(basename $(git rev-parse --show-toplevel 2>/dev/null || echo 'project'))"`
   - Push: `git push origin master`
   - Report what was captured

3. **Pull latest pack from GitHub** — In the local pack directory (`D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\`):
   - Pull: `git pull origin master`
   - If pull fails (network, auth), report the error clearly and STOP — do not proceed with stale files.

4. **Read project version** — Read `.claude/pack-version.json` to see what's currently installed.

5. **Read pack version** — Read `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\pack-info.json` to see the current pack version.

6. **Compare** — If versions match, report "Already up to date (v{version})" and stop. If the pack is older, stop with a warning. If the project is behind, proceed.

7. **Report diff** — List what will be updated:
   - `.claude/hooks/` — synced from pack
   - `.claude/skills/unity-mcp-discipline/` — synced from pack
   - `.claude/commands/` — synced from pack
   - `Assets/Editor/AgentMirror/` — synced from pack
   - `Assets/Scripts/Core/StableId.cs` — synced from pack
   - NOT updated: `.claude/teams/game-dev/index.md` (project-customisable), `CLAUDE.md`, `DESIGN.md`, `ProjectKnow/`, any game code

8. **Copy files** — For each item below, copy from pack source into project. Preserve existing project files that don't exist in the pack.
   - `cc/hooks/` → `.claude/hooks/`
   - `cc/skills/unity-mcp-discipline/` → `.claude/skills/unity-mcp-discipline/`
   - `cc/commands/` → `.claude/commands/`
   - `unity/Editor/AgentMirror/` → `Assets/Editor/AgentMirror/`
   - `unity/Runtime/StableId.cs` → `Assets/Scripts/Core/StableId.cs`

9. **Update version** — Read the pack version from `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\pack-info.json`. Write `.claude/pack-version.json` with:
   - `version`: the new pack version
   - `sourceCommit`: run `git log --oneline -1 --format=%h` in the pack dir
   - `installedAt`: current timestamp

10. **Report result** — Show what changed, the new version, and prompt the user to commit the updates.
