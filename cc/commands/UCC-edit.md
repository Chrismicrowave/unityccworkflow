# UCC-edit

Edit a UCC-pack-managed file in **both** the project `.claude/` copy AND the local pack source simultaneously. Never risk one falling behind the other.

## When to use

Run this command whenever you're about to edit a file that exists in both the project and the local pack. This covers all synced paths:

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

## Steps

1. **Identify the file** — Determine which file(s) need editing. Check the table above. If a file maps to the pack, both locations must be edited.

2. **Read both copies** — Read the project file and the corresponding pack source file. If one doesn't exist in the pack, create it. If it doesn't exist in the project but the pack has it, the project is behind — run `UCC-update` first instead.

3. **Edit both** — Apply the identical change to both files. Use `Edit` tool for each. The changes MUST be identical (except the template `[PLACEHOLDER]` differences in `CLAUDE.md` vs `CLAUDE.md.template`).

4. **Commit project** — `git add <file>` && `git commit -m "<type>: <message>"` in the Unity project repo.

5. **Commit local pack** — `cd D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\v0.1\ && git add <file> && git commit -m "<type>: <message>"` in the pack repo.

6. **Report** — Show both commit hashes and confirm both copies are in sync.
