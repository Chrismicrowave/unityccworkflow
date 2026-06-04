# UCC-update

Check the project's UCCPack sync status against the upstream repo. Reports divergences, uncommitted changes, and ahead/behind status — then suggests actions to resolve.

## Steps

1. **Auto-accept: always confirm upgrade** — The user invoked this skill, so proceed without asking "are you sure?".

2. **Pull latest pack from GitHub** — In the local pack directory (`D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\`):
   - `git checkout master && git pull origin master`
   - If pull fails (network, auth), report the error clearly and STOP.

3. **Read project version** — `.claude/pack-version.json`

4. **Read pack version** — `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\pack-info.json`

5. **Check divergence status** — For each synced path pair, check all three states:

   **a. Uncommitted project changes** — `git status --short` on the project paths:
      - `.claude/hooks/`
      - `.claude/skills/unity-mcp-discipline/`
      - `.claude/commands/UCC-*.md`
      - `Assets/Editor/AgentMirror/`
      - `Assets/Scripts/Core/StableId.cs`

   **b. Content divergence** — `git diff --no-index` between project file and pack source:
      - `.claude/hooks/` ↔ `cc/hooks/`
      - `.claude/skills/unity-mcp-discipline/` ↔ `cc/skills/unity-mcp-discipline/`
      - `.claude/commands/UCC-*.md` ↔ `cc/commands/UCC-*.md`
      - `Assets/Editor/AgentMirror/` ↔ `unity/Editor/AgentMirror/`
      - `Assets/Scripts/Core/StableId.cs` ↔ `unity/Runtime/StableId.cs`

   **c. Ahead / behind UCCPack repo** — In the pack dir:
      - `git log --oneline HEAD..origin/master` (behind — unpulled remote commits)
      - `git log --oneline origin/master..HEAD` (ahead — unpushed local commits)

6. **Report results** — Show a clean summary:
   ```
   ═══ UCCPack Sync Report ═══
   Project version: v0.5.1
   Pack version:    v0.6.0

   ── Uncommitted project changes ──
   M  .claude/hooks/pre-git-commit-guard.sh

   ── Content divergence (project vs pack) ──
   ✓ .claude/hooks/ — in sync
   ✗ .claude/commands/UCC-update.md — differs

   ── Pack repo status ──
   Local master is 1 commit ahead of origin/master

   ── Suggested actions ──
   1. Uncommitted changes found → commit project first
   2. Content diverges → run /UCC-push to capture project→pack
   3. Pack is ahead → run /UCC-update again after push to sync
   ```

7. **Decision matrix** — Based on the report, take the appropriate action:

   | Uncommitted? | Diverged? | Pack ahead? | Action |
   |---|---|---|---|
   | No | No | No | ✅ Already up to date — stop |
   | No | No | Yes | Proceed with update (sync pack→project) |
   | Yes | — | — | Suggest: commit project files first |
   | — | Yes | — | Suggest: run `/UCC-push` to capture to pack |
   | No | No | Yes | Proceed with update, ask user to confirm |
   | Mixed | Mixed | Mixed | Suggest: run `/UCC-push` first, then `/UCC-update` again |

8. **If proceeding with update** — Copy files from pack to project:
   - `cc/hooks/` → `.claude/hooks/`
   - `cc/skills/unity-mcp-discipline/` → `.claude/skills/unity-mcp-discipline/`
   - `cc/commands/` → `.claude/commands/`
   - `unity/Editor/AgentMirror/` → `Assets/Editor/AgentMirror/`
   - `unity/Runtime/StableId.cs` → `Assets/Scripts/Core/StableId.cs`
   - Preserve existing project files that don't exist in the pack.
   - Update `.claude/pack-version.json` with the new pack version.

9. **Report result** — Show what changed, the new version, and prompt the user to commit.
