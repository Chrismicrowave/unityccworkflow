# UCC-update

Update the unityccworkflow pack files in this project to the latest version — pulls the pack from GitHub first, then syncs files.

## Steps

1. **Auto-accept: always confirm upgrade** — The user invoked this skill, so proceed without asking "are you sure?".

2. **Pull latest pack from GitHub** — In the local pack directory (`D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\`):
   - Pull: `git pull origin master`
   - If pull fails (network, auth), report the error clearly and STOP — do not proceed with stale files.

3. **Read project version** — Read `.claude/pack-version.json` to see what's currently installed.

4. **Read pack version** — Read `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\pack-info.json` to see the current pack version.

5. **Check divergences** — For each synced path pair, run `git diff --no-index` to see if the project's copy has diverged from the pack. Report any differences:
   - `.claude/hooks/` ↔ `cc/hooks/`
   - `.claude/skills/unity-mcp-discipline/` ↔ `cc/skills/unity-mcp-discipline/`
   - `.claude/commands/UCC-*.md` ↔ `cc/commands/UCC-*.md`
   - `Assets/Editor/AgentMirror/` ↔ `unity/Editor/AgentMirror/`
   - `Assets/Scripts/Core/StableId.cs` ↔ `unity/Runtime/StableId.cs`

6. **Report comparison** — Show:
   - Current project version vs pack version
   - Any divergences found (which files differ)
   - Suggest actions: "Divergences found — run `UCC-push` to capture project changes to the pack first, then re-run `UCC-update`."

7. **If no divergences and versions match** — Report "Already up to date (v{version})" and stop.

8. **If no divergences and pack is newer** — Proceed with update:
   - List what will be updated:
     - `.claude/hooks/` — synced from pack
     - `.claude/skills/unity-mcp-discipline/` — synced from pack
     - `.claude/commands/` — synced from pack
     - `Assets/Editor/AgentMirror/` — synced from pack
     - `Assets/Scripts/Core/StableId.cs` — synced from pack
     - NOT updated: `.claude/teams/game-dev/index.md` (project-customisable), `CLAUDE.md`, `DESIGN.md`, `ProjectKnow/`, any game code
   
   - Copy files from pack to project:
     - `cc/hooks/` → `.claude/hooks/`
     - `cc/skills/unity-mcp-discipline/` → `.claude/skills/unity-mcp-discipline/`
     - `cc/commands/` → `.claude/commands/`
     - `unity/Editor/AgentMirror/` → `Assets/Editor/AgentMirror/`
     - `unity/Runtime/StableId.cs` → `Assets/Scripts/Core/StableId.cs`
   
   - Update `.claude/pack-version.json` with new version, sourceCommit, and timestamp.

9. **Report result** — Show what changed, the new version, and prompt the user to commit the updates.
