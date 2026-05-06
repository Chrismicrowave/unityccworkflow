# Game Dev Team — Pattern Index

Load this at session start. Read domain files on demand. **These are genre-shaped guidelines — adapt for different project goals.**

## Department Routing

| Working on... | Read |
|---------------|------|
| Game mechanics, balance, player verbs, core loop, progression | `design.md` |
| Player controller, input, combat, interaction, inventory | `gameplay.md` |
| Save/load, game state, events, singletons, checkpoints | `systems.md` |
| HUD, menus, bars, dialogue box, prompts | `ui.md` |
| NPC patrol, NavMesh, pathfinding, AI behaviours | `ai.md` |
| Animator, AnimationClips, Blend Trees, Timeline, Cinemachine | `animation.md` |
| Shaders, VFX, lighting, post-processing, materials | `graphics.md` |
| SFX, BGM, audio mixing, spatial audio | `audio.md` |
| Editor scripts, custom inspectors, build pipelines | `tools.md` |
| Testing, CI/CD, performance profiling | `qa.md` |

## Pattern Inventory (available knowledge)

These files contain reusable patterns loaded on demand. **Scan this list at session start** so you know what knowledge exists.

| File | Contains |
|------|----------|
| `animation.md` | Spider-Verse frame-rate technique (speed=0 + manual Update), Unity 6 quirk (speed must be >0 for manual Update to work), Mixamo FBX import workflow (Humanoid + same Avatar + Bake Into Pose Root Y), RootT.y curve binding, normalizedTime polling for action end, per-state FPS override pattern |
| `tools.md` | Extracting animation clips from FBX (CopySerialized), general editor utility patterns |
| `design.md` | *(not yet populated)* |
| `gameplay.md` | *(not yet populated)* |
| `systems.md` | *(not yet populated)* |
| `ui.md` | *(not yet populated)* |
| `ai.md` | *(not yet populated)* |
| `graphics.md` | *(not yet populated)* |
| `audio.md` | *(not yet populated)* |
| `qa.md` | *(not yet populated)* |

## Non-Negotiables (Unity engine — always apply)

- **Never `??` with Unity Objects** — `if (x == null)` only. Fake-nulls bypass C# reference equality.
- **Check serialized overrides** before changing code defaults. Scene/prefab values beat script defaults.
- **Stop Play mode before structural edits.** Changes don't persist.
- **UI bars: `localScale.x` or `sizeDelta.x`** — never `Image.fillAmount` without sprite.
- **Input System throughout** — Input System actions for gameplay, `InputSystemUIInputModule` on EventSystem for UI. Never legacy `StandaloneInputModule` or `Input.GetKey`.
- **Use Unity built-in UI** via `DefaultControls` or `ExecuteMenuItem("GameObject/UI/...")` — never build from primitives.
- **Frame-rate technique: `animator.speed = 0`** — when using manual `_animator.Update()`, speed must be set to 0 between steps. In Unity 6, temporarily set speed=1 before manual Update (silent no-op at speed=0).

## Sub-Agent Team (for complex multi-agent tasks only)

| Agent | Department | Pattern File |
|-------|-----------|-------------|
| `gameplay-engineer` | Gameplay Engineering | `gameplay.md` |
| `systems-engineer` | Systems Engineering | `systems.md` |
| `ui-engineer` | UI Engineering | `ui.md` |
| `ai-engineer` | AI Engineering | `ai.md` |
| `animation-engineer` | Animation Engineering | `animation.md` |
| `graphics-engineer` | Graphics Engineering | `graphics.md` |
| `audio-engineer` | Audio Engineering | `audio.md` |
| `tools-engineer` | Tools Engineering | `tools.md` |
| `qa-engineer` | QA Engineering | `qa.md` |

When spawning a specialist: "Read `.claude/teams/game-dev/{domain}.md` for patterns and `Assets/Docs/ProjectKnow/{domain}.md` for project specifics."

## Learning Protocol

During sessions, when you discover something worth keeping:
1. **Pattern** (applies to any Unity project) → propose `.claude/teams/game-dev/{domain}.md`
2. **Project** (this game only) → propose `Assets/Docs/ProjectKnow/{domain}.md`
3. **Principle-level mistake** → propose project memory (`memory/feedback_*.md`)
4. **Always ask before writing.** Never save silently.
