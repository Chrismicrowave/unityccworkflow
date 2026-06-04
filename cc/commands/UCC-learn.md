# UCC-learn

Extract patterns and project knowledge from the current Unity project and backport them into the UnityCCWorkflow pack. Run this after any productive session to persist learnings.

## Steps

1. **Scan session context** — Review recent commits, uncommitted changes, and active discussion topics. Identify:
   - Bugs fixed and their root causes
   - New features or systems built
   - Workarounds for engine quirks
   - Unity API patterns discovered (correct field names, serialization gotchas, etc.)

2. **Categorize each learning** by audience and density:
   - **CLAUDE.md rule** (1-2 line high-impact rule needed every session) → `CLAUDE.md` Code Rules section
     - Criteria: concise enough to scan, applies to every session, no code examples needed
     - Also add the full detail to the relevant `.claude/teams/game-dev/{domain}.md` if it has depth beyond the one-liner
   - **Project-specific** (this prototype only) → `Assets/Docs/ProjectKnow/{domain}.md` — update the relevant file or create new
   - **General pattern** (applies to any Unity project) → `.claude/teams/game-dev/{domain}.md` — add full detail with code examples, edge cases, rationale
   - **Principle-level mistake** → project memory (`memory/feedback_*.md`)

   **Decision guide:**
   | Density | Audience | Destination |
   |---------|----------|-------------|
   | 1-2 line rule, universal | Every session reminder | CLAUDE.md + domain detail |
   | Code examples, edge cases, rationale | On-demand reference | `.claude/teams/game-dev/*.md` |
   | Project-specific | This project only | `Assets/Docs/ProjectKnow/` |
   | AI-behaviour correction | Future same-AI sessions | `memory/feedback_*.md` |

3. **Read existing docs** — Before writing, read the target file to check if the knowledge is already captured. If partially covered, merge; if fully covered, skip.

4. **Write docs + update CLAUDE.md** — For each new or updated doc, present a diff summary to the user before saving:
   > "Found [N] new learnings: [list]. Shall I save to [file paths]?"
   - If a learning goes to CLAUDE.md, slot it into the Code Rules section (alphabetically by topic)
   - If a learning goes to a domain file AND CLAUDE.md, add a one-liner pointer in CLAUDE.md and full detail in the domain file
   - Wait for user confirmation before writing.

5. **Backport domain files to pack** — Copy updated `.claude/teams/game-dev/{domain}.md` files to `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\cc\teams\game-dev\{domain}.md` so future projects get the patterns.

5b. **Backport CLAUDE.md template to pack** — Read the project's `CLAUDE.md` and the pack's `templates/CLAUDE.md.template`. Identify any Code Rules in the project that are general (not project-specific) and missing from the template. Present and ask:
    > "These [N] rules in your CLAUDE.md aren't in the pack template. Are they general enough that all Unity projects should get them? Backport to pack template?"
    - If yes: add each rule as a new bullet in the template's Code Rules section, then report
    - If no: mark it as project-specific and suggest moving it to `Assets/Docs/ProjectKnow/` instead

    **How to distinguish general vs project-specific:**
    | General (backport) | Project-specific (skip) |
    |---|---|
    | "Never `??` with Unity Objects" | "FrameRateSampler: animator.speed = 0" |
    | "Always use the new Input System" | "Our custom EventBus dispatch pattern" |
    | "Wire references via MCP, never ask" | "Use StrokeSnap-specific brush API" |
    | Unity engine rules | Project architecture conventions |
    | MCP/workflow discipline | Prototype-specific gotchas |

6. **Report** — Summarise:
   - Which files were updated/created
   - What was learned
   - Which new rules were added to the pack template (if any)
   - Which domain files were backported
