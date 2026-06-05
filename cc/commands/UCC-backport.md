# UCC-backport

Check which project-level UCCPack files have diverged from the local pack, analyze the direction (which side is newer), present findings with recommendations, and backport only what you approve. Project-specific info (MCP servers, game scripts, scene data) is excluded automatically.

Run before `UCC-push` if the project has changes you want to save back to the pack.

## Flow

```
UCC-backport  →  review + approve divergences → local pack
UCC-push      →  version + push to GitHub
UCC-update    →  sync pack → project
```

## Steps

### 1. Check divergences

For each tracked path, run `git diff --no-index -w` (ignore whitespace) between the project copy and the pack source:

| Project path | Pack path | What it is |
|---|---|---|
| `.claude/hooks/` | `cc/hooks/` | Hook scripts |
| `.claude/commands/UCC-*.md` | `cc/commands/UCC-*.md` | UCC command definitions |
| `.claude/skills/unity-mcp-discipline/` | `cc/skills/unity-mcp-discipline/` | Skill files |
| `Assets/Scripts/Editor/AgentMirror/` | `unity/Editor/AgentMirror/` | Editor utilities |
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

### 2. Determine direction for each divergent file

Do NOT just present raw diffs. For each divergent file, systematically determine which side is ahead:

**a. If file exists on one side but not the other:**
- Check if it's a **rename** — look for matching internal class names, similar line counts (±10%), similar logic structure
- Check git commit history on both sides for the file and its potential equivalents
- For AgentMirror specifically: files often get renamed during refactors. Cross-reference `public static class Name` against all filenames in the counterpart directory
- If it IS a rename: the newer name side is ahead, the old-name version should be deleted from the other side
- If it's genuinely new: check `git log --oneline` on the file to see when it was created

**b. If file exists on both sides:**
- Check `git log --oneline -1` for each file to compare recency
- If commits are close in time, compare actual content (diff size, added vs removed lines)
- A large `+` diff on the project side with no meaningful `+` on the pack side = project ahead (safe to backport)
- A large `+` diff on the pack side = pack ahead (project needs UCC-update, not backport)
- Lines changed on both sides = both ahead (manual merge needed)

**c. Direction classification:**
- **Project ahead** → backport: copy project → pack
- **Pack ahead** → skip: project needs `UCC-update` (pull from pack)
- **Renamed** → sync: copy project version to pack, delete old-name file from pack
- **Stale renamed copy** → delete from pack: pack has old-name file that project renamed and improved
- **Both changed** → flag for manual merge

### 3. Present diff summary with recommendations

Show each divergent file with direction analysis, evidence, and a clear recommendation:

```
── Divergent files ──

[ahead project→pack]  post-edit-script.sh
    Evidence: project=abc123 (today), pack=def456 (May 6)
    → Backport: added codegraph sync block

[renamed (pack stale)]  AnimatorDumpEmitter.cs
    Evidence: class name != any project file, but matches AnimatorsSnapshotEmitter (same 261 vs 264 lines, identical internal class `AnimatorAssetPostprocessor`)
    → Delete stale old-name from pack, project has current version

[NEW]  codegraph-vs-lsp-mcp.md  *(deleted — lsp-mcp removed from project)*

── Skipped (project-specific) ──
  settings.json — MCP server config (pack only tracks hooks section)

── Skipped (pack ahead) ──
  AgentMirror/ProjectIndexEmitter.cs — pack has newer version, needs UCC-update
```

### 4. Ask for approval

Present your analysis and ask the user:
- Backport? (yes/no per file or in groups)
- Or backport all / skip all / backport recommended only
- For renaming resolutions: confirm the mapping before deleting files
- Use plain text — do not use AskUserQuestion tool

### 5. Sync hooks registry in README

After backporting hook files, check if `cc/hooks/` has any files not listed in `README.md`'s hooks table (or listed but no longer present). If out of sync, update the table to match actual hook files. This keeps the registry accurate.

### 6. Backport approved files

Copy approved files from project → pack source path. For settings.json, extract only the `"hooks"` section. For CLAUDE.md, extract only the Code Rules section.

For **renamed/stale files**: delete the old-name file from the pack. The current project version is the canonical one.

### 7. Commit in pack dir

`git add -A && git commit -m "backport: $(basename $(git rev-parse --show-toplevel 2>/dev/null || echo 'project')) — $(date +%Y-%m-%d)"`

### 8. Report

Show what was backported and what was skipped. Prompt: "Run `UCC-push` to version and push to GitHub."
