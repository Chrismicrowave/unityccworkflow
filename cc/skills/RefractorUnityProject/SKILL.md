---
name: RefractorUnityProject
description: Full project-wide refactor of a Unity project to production quality — code, assets, scene hierarchy, editor wiring. Staged execution with role-based agents, superpowers planning, and architecture doc update.
context: fork
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, Agent, Task
---

# /RefractorUnityProject

Full project-wide refactor to production quality. Covers code, asset structure, scene hierarchy, prefabs, and editor wiring. Staged, role-based, plan-first execution.

## Constraints (NON-NEGOTIABLE)

- **Zero gameplay/UX behavior changes.** The game must work identically from the player's perspective before and after.
- **Bug fixes allowed only if they do not affect any player interaction.** If a bug fix could change any observable player behavior, FLAG IT and skip — do not fix silently.
- **Flag anything uncertain** — surface it to the user before acting, never guess on ambiguous refactors.
- **Prefabs**: only convert to prefab where it genuinely makes sense (shared/reusable GOs). Do not be aggressive — prefer leaving things as-is if benefit is marginal.
- **Never delete shaders without checking all material (.mat) files** for GUID references. A shader may be "unreferenced" in scenes/prefabs but still used by a material.
- **New code should be plain C# classes** (not MonoBehaviours) where possible. Avoid creating new MonoBehaviour scripts that need Inspector wiring.

## Scope

1. **Code** — naming, structure, decoupling, modularisation, dead code removal, comment cleanup
2. **Asset folders** — consistent naming conventions, logical grouping, no stray files
3. **Scene hierarchy** — logical grouping, naming, tag/layer hygiene
4. **Editor wiring** — missing references, duplicate components, stale serialized fields, prefab candidates
5. **Architecture doc** — update `docs/architecture.md` at the very end to reflect final state

## Execution Protocol

### Step 0 — Branch
Confirm a dedicated refactor branch exists (create one if not: `git checkout -b refractor`).
Push the branch to origin: `git push -u origin refractor`.

### Step 1 — Align Gameplay Understanding (CRITICAL)

**Purpose**: Establish shared understanding of the game BEFORE touching any code. This prevents the most common source of refractor bugs — the AI guessing how a system works and breaking it.

1. **Ask the user to describe the game** in their own words (plain text, not AskUserQuestion). Prompt:
   > "Describe what the player does each turn, what systems are involved, and what the key architectural decisions are (e.g. camera system, UI framework, state machine). I'll confirm my understanding."

2. **Confirm by rephrasing** — restate the understanding back to the user covering:
   - Core gameplay loop (turn by turn)
   - UI architecture (canvas types, camera transitions, world space vs screen space)
   - Key systems (board, reels, combat, NPC AI)
   - What NOT to touch (cameras, canvas settings, scene references)
   Wait for user confirmation before proceeding.

3. **Capture everything** — save gameplay understanding, systems map, and key constraints to `docs/refractor-plan.md`.

### Step 2 — Snapshot Editor State (CRITICAL)

**Purpose**: Record the full editor wiring state BEFORE making any changes. This snapshot is the safety net — if something breaks, you can compare and restore.

1. **Ensure Unity Editor is open** and NOT in Play Mode. Run `stop_game` via MCP first.

2. **Capture scene hierarchy** — use MCP `list_game_objects_in_hierarchy` with `includeInactive=true`, `onlyPaths=true`. Save output.

3. **Capture key GameObject info** — for every root-level GameObject with a custom script component, run MCP `get_game_object_info` with no component filter. Include at minimum:
   - GameManager (all component fields)
   - Board
   - Each canvas (MainMenuCanvas, GameCanvas, SettingCanvas, HoverCanvas, BGCanvas, ScreenSpaceCanvas)
   - Main Camera
   - UICameraManager
   - AudioManager
   - DOTweenManager
   - SettingsManager

4. **Capture camera setup** — for each CinemachineCamera, run `get_game_object_info`.

5. **Capture build settings** — read `ProjectSettings/EditorBuildSettings.asset` for scene list.

6. **Save snapshot to `docs/refractor-snapshot.md`** — a structured doc with all captured data. Format:
   ```
   # Refractor Snapshot — <ProjectName> — <date>
   ## Gameplay Understanding
   (from Step 1)
   ## Scene Hierarchy
   (from step 2.2)
   ## GameObjects
   ### GameManager
   - board: Board (path)
   - dotweenManager: DOTweenManager (path)
   - ... all fields
   ### MainMenuCanvas
   - Canvas: WorldSpace, scaleFactor: 1.0
   - RectTransform: pos (-1.9, 10, 7.6), scale (0.01, 0.01, 0.01)
   - ...
   ## Camera Setup
   ### CM vcam1 - GameBoard
   - Priority: ...
   ## Build Settings
   - Scene 0: Assets/MainGame.unity
   ```

7. **Commit and push the snapshot** — `git add -A && git commit -m "docs: add refractor snapshot — pre-refactor editor state + gameplay understanding" && git push`

### Step 3 — Analyse
Spawn an `Explore` subagent (thoroughness: very thorough) to survey:
- All scripts (structure, coupling, naming)
- Asset folder layout
- Scene hierarchy
- Existing docs (architecture.md, CLAUDE.md)

### Step 4 — Plan
Use `superpowers:writing-plans` to produce a staged refactor plan. Append to `docs/refractor-plan.md` (after the gameplay understanding from Step 1). Plan must include:
- Numbered stages with clear scope per stage
- Risk level per stage (Low / Medium / High)
- Flags list (potential behavior-affecting changes to review with user)
- Architecture doc update as final stage

### Step 5 — User Review
Present the plan summary and flags list. Wait for user approval before proceeding.

### Step 6 — Execute Stage by Stage
For each stage:
- Spawn appropriate specialist agent(s): `code-cleanup-optimizer`, `code-reviewer`, `codebase-context-analyzer`, `documentation-expert`, or general-purpose
- Use Unity MCP (`coplay-mcp`) tools for all editor/scene/hierarchy changes — never ask user to do manual Editor work unless MCP truly cannot do it
- Commit after each stage: `git commit -m "refactor(<scope>): <description>"`
- Report stage complete, summarise what changed, before moving to next
- **After each stage**, verify against the snapshot: run `check_compile_errors`. If any stage produces compile errors or breaks snapshot-verified references, STOP and fix before proceeding.

### Step 7 — Token Window Guard
Monitor context usage. At ~90% of token window(claude code 5h session limit, and weekly limit): **STOP**, commit current work, save progress notes to `docs/refractor-progress.md`, and prompt user to continue in a new session with `/RefractorUnityProject continue`.

### Step 8 — Final Verification (CRITICAL)

**Purpose**: Ensure the refactored project works out of the box by comparing against the pre-refactor snapshot.

1. **Compile check** — run `check_compile_errors`. Must be zero errors before proceeding.
2. **Play Mode test** — enter Play Mode via MCP. Wait 2 seconds. Check Unity logs for errors. Stop Play Mode.
3. **Snapshot comparison** — spot-check key GameObjects from `docs/refractor-snapshot.md`:
   - Verify all GameManager Inspector references still resolve (board, dotweenManager, actionPanel, etc.)
   - Verify canvas transforms, render modes, and scalers match the snapshot
   - Verify camera priorities and transforms match
   - Verify no Missing Script warnings in Unity logs
4. **Report any discrepancies** to the user with exact before/after values. Do NOT fix silently — ask.

### Step 9 — Architecture Doc Update (Final Stage)
Update `docs/architecture.md` to reflect:
- New folder/file structure
- Any renamed systems or components
- Prefabs added
- Decoupling changes

Commit: `git commit -m "docs: update architecture.md post-refactor"`

### Step 10 — Final Commit & Push
Ensure everything is committed and pushed: `git add -A && git commit -m "refactor: final post-verification touch-ups" && git push`
Report to user: "Refractor complete. Branch `refractor` is pushed. Snapshot at `docs/refractor-snapshot.md` for post-merge verification."

## Critical Lessons (from MemeFlip refractor, June 2026)

### Scene Corruption on save_scene
- **Never call `save_scene()` mid-refactor after restoring a scene from git.** Unity may hold a corrupted in-memory version with lost references and reset transforms. `save_scene()` persists that corruption to disk.
- **Safe workflow:** after `git checkout <branch> -- Assets/Scene.unity`, immediately call MCP `open_scene` to force Unity to reload from disk. Verify key references before continuing.
- If corruption already happened: re-restore from git, open_scene, verify, then carefully save.

### State Machine Transition Discipline
- When extracting a state machine, **every** direct assignment of a phase/state variable must go through the state transition method — including legacy code paths. A single bypass causes `_currentState` to desync from `currentPhase`, leading to NREs on the next state transition.
- **Checklist**: grep for every `currentPhase = TurnPhase.X` and `phase = X` in the refactored file. Replace all of them (except the initial declaration) with TransitionTo calls.

### Asset Move Protocol
- **Always move files WITH their .meta files.** The GUID in the .meta IS the identity — file path is irrelevant to Unity references. Moving without .meta = broken GUID = pink textures, missing scripts, lost references.
- **NEVER use duplicate+delete** for moving assets. A duplicate generates a new GUID. Always use `mv file.ext dir/ && mv file.ext.meta dir/`.
- Git automatically tracks renames if both file and .meta move together.

### Folder Convention (Standard Unity)
```
Sprites/   (was Image/, Textures/)
Audio/     (was Sound/)
Fonts/     (was Font/)
Materials/ (all .mat files in one place)
Shaders/   (at root, not nested under Scripts/)
Settings/  (pipeline assets, ScriptableObjects)
```

### Comment Discipline
- Every `#region` gets a descriptive one-line comment between the ═══ separators:
  ```
  // ═══════════════════════════════════
  //  Section Name — what this section does in 2-6 words
  // ═══════════════════════════════════
  ```
- Class-level XML doc comments summarize responsibilities, not implementation.

### Domain Reload Reference Loss
- Unity can lose serialized component references (public fields in the Inspector) during domain reloads triggered by script changes. This happens silently.
- **Always verify** key Inspector references after compilation (via MCP `get_game_object_info` or manually).

## Continue Mode

When invoked as `/RefractorUnityProject continue`:
- Read `docs/refractor-plan.md` and `docs/refractor-progress.md`
- Resume from the last incomplete stage
- Do not re-analyse already-completed stages

## Output

After all stages complete:
- Summary of all changes made
- List of flagged items not actioned (for user review)
- Confirmation that `docs/architecture.md` is updated
- PR-ready commit history on the refractor branch
