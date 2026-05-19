# UCC-learn

Extract patterns and project knowledge from the current Unity project and backport them into the UnityCCWorkflow pack. Run this after any productive session to persist learnings.

## Steps

1. **Scan session context** — Review recent commits, uncommitted changes, and active discussion topics. Identify:
   - Bugs fixed and their root causes
   - New features or systems built
   - Workarounds for engine quirks
   - Unity API patterns discovered (correct field names, serialization gotchas, etc.)

2. **Categorize each learning**:
   - **Project-specific** (this prototype only) → `Assets/Docs/ProjectKnow/{domain}.md` — update the relevant file or create new
   - **General pattern** (applies to any Unity project) → `.claude/teams/game-dev/{domain}.md` — update or create
   - **Principle-level mistake** → project memory (`memory/feedback_*.md`)

3. **Read existing docs** — Before writing, read the target file to check if the knowledge is already captured. If partially covered, merge; if fully covered, skip.

4. **Write docs** — For each new or updated doc, present a diff summary to the user before saving:
   > "Found [N] new learnings: [list]. Shall I save to [file paths]?"
   - Wait for user confirmation before writing.

5. **Backport to pack** — Copy updated `.claude/teams/game-dev/{domain}.md` files to `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\v0.1\cc\teams\game-dev\{domain}.md` so future projects get the patterns.

6. **Report** — Summarise:
   - Which files were updated/created
   - What was learned
   - Whether pack was backported
