# unityccworkflow-upgrade

Upgrade the unityccworkflow pack files in this project to the latest version from the local pack.

## Steps

1. **Read project version** — Read `.claude/pack-version.json` to see what's currently installed.

2. **Read pack version** — Read `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\v0.1\pack-info.json` to see the current pack version.

3. **Compare** — If versions match, report "Already up to date (v{version})" and stop. If the pack is older, stop with a warning. If the project is behind, proceed.

4. **Report diff** — List what will be updated:
   - `.claude/hooks/` — synced from pack
   - `.claude/skills/unity-mcp-discipline/` — synced from pack
   - `.claude/commands/` — synced from pack
   - `Assets/Editor/AgentMirror/` — synced from pack
   - `Assets/Scripts/Core/StableId.cs` — synced from pack
   - NOT updated: `.claude/teams/game-dev/index.md` (project-customisable), `CLAUDE.md`, `DESIGN.md`, `ProjectKnow/`, any game code

5. **Copy files** — For each item below, copy from pack source into project. Preserve existing project files that don't exist in the pack.
   - `cc/hooks/` → `.claude/hooks/`
   - `cc/skills/unity-mcp-discipline/` → `.claude/skills/unity-mcp-discipline/`
   - `cc/commands/` → `.claude/commands/`
   - `unity/Editor/AgentMirror/` → `Assets/Editor/AgentMirror/`
   - `unity/Runtime/StableId.cs` → `Assets/Scripts/Core/StableId.cs`

6. **Update version** — Write the new version to `.claude/pack-version.json`.

7. **Report result** — Show what changed, the new version, and prompt the user to commit the upgrades.
