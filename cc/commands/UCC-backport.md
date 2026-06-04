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

   The following are **never** suggested — they're project-specific:
   - MCP server config in `settings.json`
   - Game scripts (`Assets/Scripts/Systems/`, `UI/`, etc.)
   - Scene data (`.unity`, `.prefab`)
   - `Assets/Docs/`
   - `.gitignore`, `.git/`
   - `CLAUDE.md` project description / context (only Code Rules are candidates)

2. **Present diff summary** — Show each divergent file with a brief description of what changed:
   ```
   ── Divergent files ──
   [hooks]  pre-tool-use-write-guard.sh  — added scene-save signal
   [hooks]  pre-git-commit-guard.sh      — new file
   [cmds]   UCC-backport.md              — new file
   [rules]  CLAUDE.md → template         — "save before MCP write" rule added

   ── Skipped (project-specific) ──
   settings.json — MCP server config (pack only tracks hooks section)
   CLAUDE.md project description — not a Code Rule
   ```

3. **Ask for approval** — For each divergent file, ask the user:
   - Backport? (yes/no per file)
   - Or backport all / skip all
   - Use plain text — do not use AskUserQuestion tool

4. **Backport approved files** — Copy approved files from project → pack source path. For settings.json, extract only the `"hooks"` section. For CLAUDE.md, extract only the Code Rules section.

5. **Commit in pack dir** — `git add -A && git commit -m "backport: $(basename $(git rev-parse --show-toplevel 2>/dev/null || echo 'project')) — $(date +%Y-%m-%d)"`

6. **Report** — Show what was backported and what was skipped. Prompt: "Run `UCC-push` to version and push to GitHub."
