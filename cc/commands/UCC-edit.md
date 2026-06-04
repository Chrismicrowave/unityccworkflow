# UCC-edit

**Note: Mostly superseded by `UCC-backport`** — if you already edited the file in the project, just run `UCC-backport` to capture it. UCC-edit is only needed when you want to edit the project and pack versions in lockstep simultaneously.

Edit a UCC-pack-managed file in **both** the project `.claude/` copy AND the local pack source simultaneously. Never risk one falling behind the other.

## When to use

Run this when you're about to edit a file you know exists in both the project and the local pack, and want both updated at the same moment. This covers all synced paths:

| Project path | Pack source path |
|---|---|
| `.claude/commands/UCC-*.md` | `cc/commands/` |
| `.claude/skills/unity-mcp-discipline/*` | `cc/skills/unity-mcp-discipline/` |
| `.claude/hooks/*` | `cc/hooks/` |
| `.claude/teams/game-dev/*.md` | `cc/teams/game-dev/` |
| `Assets/Editor/AgentMirror/*` | `unity/Editor/AgentMirror/` |
| `Assets/Scripts/Core/StableId.cs` | `unity/Runtime/StableId.cs` |
| `CLAUDE.md` | `templates/CLAUDE.md.template` |

If the file to edit is NOT in this table, do NOT run this command — it's a project-only file.

### Critical rule links

The `cc/teams/game-dev/*.md` files contain full pattern documentation. Their condensed one-line versions live in `CLAUDE.md`'s **Code Rules** section. These must stay in sync:

| Full pattern (pack) | Condensed rule (CLAUDE.md) |
|---|---|
| `cc/teams/game-dev/systems.md` | `CLAUDE.md` → Code Rules section |
| (any future `cc/teams/game-dev/*.md` with a rule) | `CLAUDE.md` → Code Rules section |

When adding/removing/updating a critical rule in a pack domain file, always update the corresponding rule in CLAUDE.md's Code Rules. When editing a Code Rule in CLAUDE.md, always update the full pattern in the pack domain file.

## Steps

1. **Identify the file** — Determine which file(s) need editing. Check the table above. If a file maps to the pack, both locations must be edited.

2. **Read both copies** — Read the project file and the corresponding pack source file. If one doesn't exist in the pack, create it. If it doesn't exist in the project but the pack has it, the project is behind — run `UCC-update` first instead.

3. **Edit both** — Apply the identical change to both files. Use `Edit` tool for each. The changes MUST be identical (except the template `[PLACEHOLDER]` differences in `CLAUDE.md` vs `CLAUDE.md.template`).

4. **Sync critical rules** — If the edited file is a pack domain file (`cc/teams/game-dev/*.md`) or `CLAUDE.md`/`CLAUDE.md.template`, check whether a critical rule was added/removed/updated. If so, update the other end:
   - Pack rule changed → update the condensed rule in both `CLAUDE.md` and `templates/CLAUDE.md.template`
   - CLAUDE.md rule changed → update the full pattern in the corresponding `cc/teams/game-dev/*.md` and its project copy

5. **Commit project** — `git add <file>` && `git commit -m "<type>: <message>"` in the Unity project repo.

6. **Commit local pack** — `cd D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\ && git add <file> && git commit -m "<type>: <message>"` in the pack repo.

7. **Report** — Show both commit hashes and confirm both copies are in sync.
