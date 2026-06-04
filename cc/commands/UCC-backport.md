# UCC-commit

Capture all project-level UCCPack divergences back to the local UCCPack directory. This is the **first step** before `UCC-push` — it syncs everything from the project into the local pack so you can then version and push.

Run this after making project changes to hooks, settings, CLAUDE.md, or other synced files.

## Flow

```
UCC-commit  →  capture project→pack locally
UCC-push    →  version + push to GitHub
UCC-update  →  sync pack→project
```

## Steps

1. **Check all tracked paths** for divergences (project vs local pack):

   | Project path | Pack path | Sync mode |
   |---|---|:---:|
   | `.claude/hooks/` | `cc/hooks/` | overwrite |
   | `.claude/commands/UCC-*.md` | `cc/commands/UCC-*.md` | overwrite |
   | `.claude/skills/unity-mcp-discipline/` | `cc/skills/unity-mcp-discipline/` | overwrite |
   | `Assets/Editor/AgentMirror/` | `unity/Editor/AgentMirror/` | overwrite |
   | `Assets/Scripts/Core/StableId.cs` | `unity/Runtime/StableId.cs` | overwrite |
   | `.claude/settings.json` | `cc/settings.json.pack` | extract + merge |
   | `CLAUDE.md` | `templates/CLAUDE.md.template` | rules sync |

2. **For overwrite paths** — use `git diff --no-index -w` (ignore whitespace). If files differ, copy project → pack.

3. **For `.claude/settings.json`** — extract UCCPack-specific hook matcher entries from the project's `settings.json` and merge them into `cc/settings.json.pack`. The pack only stores the `"hooks"` section's `PreToolUse` and `PostToolUse` arrays (the hook matchers), not the project-specific MCP server config.

4. **For `CLAUDE.md`** — scan the project's `CLAUDE.md` for **Code Rules** that are UCCPack-sourced (matching entries in `templates/CLAUDE.md.template`). Copy any new/changed rules to the template. This is a text merge, not an overwrite.

5. **Commit in pack dir** — `git add -A && git commit -m "capture: sync from $(basename $(git rev-parse --show-toplevel 2>/dev/null || echo 'project'))"`

6. **Report** — Show which files were captured, which were unchanged, and any merge conflicts.
