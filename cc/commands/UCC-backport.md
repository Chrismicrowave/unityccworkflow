# UCC-backport

Check which project-level UCCPack files have diverged from the local pack, present the diff for review, and backport only what you approve. Project-specific info (MCP servers, game scripts, scene data) is excluded automatically.

Run before `UCC-push` if the project has changes you want to save back to the pack.

## Flow

```
UCC-backport  →  review + approve divergences → local pack
UCC-push      →  version + push to GitHub
UCC-update    →  sync pack → project
```

## Steps

1. **Check divergences** — For each tracked path, run `git diff --no-index -w` (ignore whitespace) between the project copy and the pack source:

   | Project path | Pack path | What it is |
   |---|---|---|
   | `.claude/hooks/` | `cc/hooks/` | Hook scripts |
   | `.claude/commands/UCC-*.md` | `cc/commands/UCC-*.md` | UCC command definitions |
   | `.claude/skills/unity-mcp-discipline/` | `cc/skills/unity-mcp-discipline/` | Skill files |
   | `Assets/Editor/AgentMirror/` | `unity/Editor/AgentMirror/` | Editor utilities |
   | `Assets/Scripts/Core/StableId.cs` | `unity/Runtime/StableId.cs` | Runtime component |
   | `.claude/settings.json` (hooks section) | `cc/settings.json.pack` | Hook matchers only |
   | `CLAUDE.md` (Code Rules) | `templates/CLAUDE.md.template` | Rules section only |
   | `.claude/teams/game-dev/*.md` | `cc/teams/game-dev/*.md` | Domain pattern files |

   The following are **never** suggested — they're project-specific:
   - MCP server config in `settings.json`
   - Game scripts (`Assets/Scripts/Systems/`, `UI/`, etc.)
   - Scene data (`.unity`, `.prefab`)
   - `Assets/Docs/`
   - `.gitignore`, `.git/`
   - `CLAUDE.md` project description / context (only Code Rules are candidates)

2. **Analyze direction** — For each divergent file, determine whether the project or pack is ahead:
   - **Project ahead** (new content in project, pack missing it) → safe to backport
   - **Pack ahead** (new content in pack, project missing it) → project needs update, not backport
   - **Both changed** (content diverged in both directions) → manual merge needed

   Use diff sizes to gauge: compare line counts and diff hunk sizes. A large `+` diff on the project side with no corresponding `+` on the pack side = project ahead.

3. **Present diff summary** — Show each divergent file with direction analysis:
   ```
   ── Divergent files ──
   [ahead project→pack]  cinemachine.md  — 24 lines added (OutputChannel bitmask pattern)
   [ahead project→pack]  systems.md      — 65 lines added (emitter guard, per-player orbit)
   [new]                 tools.md        — doesn't exist in pack yet (hook-editor signal pattern)
   
   ── Skipped (project-specific) ──
   settings.json — MCP server config (pack only tracks hooks section)
   CLAUDE.md project description — not a Code Rule
   ```

3. **Ask for approval** — For each divergent file, ask the user:
   - Backport? (yes/no per file)
   - Or backport all / skip all
   - Use plain text — do not use AskUserQuestion tool

4. **Sync hooks registry in README** — After backporting hook files, check if `cc/hooks/` has any files not listed in `README.md`'s hooks table (or listed but no longer present). If out of sync, update the table to match actual hook files. This keeps the registry accurate.

5. **Backport approved files** — Copy approved files from project → pack source path. For settings.json, extract only the `"hooks"` section. For CLAUDE.md, extract only the Code Rules section.

5. **Commit in pack dir** — `git add -A && git commit -m "backport: $(basename $(git rev-parse --show-toplevel 2>/dev/null || echo 'project')) — $(date +%Y-%m-%d)"`

6. **Report** — Show what was backported and what was skipped. Prompt: "Run `UCC-push` to version and push to GitHub."
