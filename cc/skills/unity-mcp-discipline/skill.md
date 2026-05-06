# unity-mcp-discipline

**First:** Check `.claude/unity-mode.json`. If `enabled: false`, ignore all rules below for this session.

## Core rules

1. **Scene orientation** — Read `Library/AgentMirror/SceneMirror.json` once at task start. Do not call `list_game_objects_in_hierarchy` unless `SceneMirror.meta.json` shows emittedAt > 5 minutes ago.

2. **Identity** — Resolve targets by stableId. If you don't have one, read `SceneMirror.json` to find it. Never address GameObjects by name/path alone.

3. **Compile discipline** — After any `.cs` edit batch, run `check_compile_errors` exactly once. On any error: STOP and surface the full error list to the user. Do not attempt to auto-fix in a loop.

4. **Behavior debugging** — Before debugging FSM/animator/dialogue behavior, read `Library/AgentMirror/AnimatorDump.json` and the relevant `DESIGN.md` section. Do not guess from source code.

5. **Prefab vs instance** — If the user says "edit X" and X exists as both a prefab asset and a scene instance, ASK which before proceeding.

6. **CHALLENGE** — Before proposing any change that contradicts `DESIGN.md` (any `<!-- stability: locked -->` or `<!-- stability: settled -->` section), STOP and present:
   > "This contradicts DESIGN.md §[section] (which says [Y]). Options: (a) update doc + follow new intent, (b) follow doc + ignore request, (c) one-off override without doc change."

7. **AMEND** — If user message contains `[INTENT-CHANGE-CANDIDATE]` (injected by hook) or explicit intent-change language ("actually", "from now on", "I want X to", "change the rule"), propose a `DESIGN.md` diff BEFORE touching code. On confirmation: update doc first, then implement.

8. **Play mode** — Check `EditorApplication.isPlaying` state before any scene edit. If game is running, stop it first unless user explicitly says to edit during play.

9. **Animator state** — When asked why a runtime behavior is happening, read `AnimatorDump.json` and `InspectorRefs.json` before reading C# source. The answer is often in a transition condition or a UnityEvent wiring, not in code.

## CHALLENGE flow (detailed)

When you identify a CHALLENGE situation:
1. STOP immediately. Do not write any code yet.
2. Quote the relevant DESIGN.md section (copy the exact text).
3. Explain how your proposed change would contradict it.
4. Present the three options labeled (a), (b), (c).
5. Wait for the user's choice. If no response in same message, assume (b) — follow the doc.

## AMEND flow (detailed)

When [INTENT-CHANGE-CANDIDATE] is in context OR you identify intent-change language:
1. Identify which DESIGN.md section(s) would be affected.
2. Propose the diff in fenced markdown: what the section says now, what it would say after.
3. Ask: "Shall I update DESIGN.md with this change before implementing?"
4. On confirmation: write DESIGN.md first. Then write code. Both in the same response.
5. If the section is `locked`: extra confirmation step — "This section is marked locked. Are you sure?"

## Stability tier cheatsheet

- `locked` → CHALLENGE always, need explicit (a) or (c) to proceed
- `settled` → CHALLENGE and offer options
- `in-flux` → note the conflict, log it, proceed with your best interpretation
- `TBD` → ask user to define before implementing
