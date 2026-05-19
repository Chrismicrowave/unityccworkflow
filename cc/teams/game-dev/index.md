# Game Dev Team — Pattern Index

Load this at session start. Read domain files on demand. **These are technique-prototype patterns — adapt for different project goals.**

## Department Routing

| Working on... | Read |
|---------------|------|
| Player controller, input, movement, state machines | `gameplay.md` |
| Animator, AnimationClips, Blend Trees, frame-rate sampling | `animation.md` |
| Cinemachine 3 (Unity 6): Follow/LookAt properties, priority, camera takeover | `cinemachine.md` |
| Save/load, game state, events, singletons, measurement/sampling | `systems.md` |
| HUD, debug UI, Inspector widgets, sliders | `ui.md` |
| NPC controllers, NavMesh, pathfinding | `ai.md` |
| Editor scripts, custom inspectors, build pipelines, asset processors | `tools.md` |
| Shaders, fullscreen effects, Sobel kernels, texel size, noise wiggle | `shaders.md` |
| Testing, performance profiling, frame timing | `qa.md` |

## Pattern Inventory (available knowledge)

These files contain reusable patterns loaded on demand. **Scan this list at session start** so you know what knowledge exists.

| File | Contains |
|------|----------|
| `animation.md` | Spider-Verse frame-rate technique (speed=0 + manual Update), Unity 6 quirk (speed must be >0 for manual Update to work), Mixamo FBX import workflow (Humanoid + same Avatar + Bake Into Pose Root Y), RootT.y curve binding, normalizedTime polling for action end, per-state FPS override pattern |
| `cinemachine.md` | Cinemachine 3 Follow/LookAt are C# properties not SerializedProperty, scene-save requirement for editor script changes, MCP tool set_property limitations, Resources.FindObjectsOfTypeAll finds inactive cameras, GameObject.Find name-only search, priority-based camera takeover |
| `gameplay.md` | *(not yet populated)* |
| `systems.md` | Script-controlled initial active state (Awake/Start, not scene toggle), RefHub pattern |
| `ui.md` | *(not yet populated)* |
| `ai.md` | *(not yet populated)* |
| `shaders.md` | Texel size calculation (never `_ScreenParams.zw`), depth Sobel kernel, single-pass wiggle, FPS quantization, FullScreenPassRendererFeature setup, noise texture requirements |
| `tools.md` | *(not yet populated)* |
| `qa.md` | *(not yet populated)* |

## Non-Negotiables (Unity engine — always apply)

- **Never `??` with Unity Objects** — `if (x == null)` only. Fake-nulls bypass C# reference equality.
- **Check serialized overrides** before changing code defaults. Scene/prefab values beat script defaults.
- **Never edit GameObjects, components, or scene references while Play Mode is active.** Changes made during Play Mode are discarded when Play Mode stops. Always call `stop_game` (or have the user stop Play Mode) before using `set_property`, `set_transform`, `add_component`, or any MCP tool that modifies scene objects. To check: `get_unity_editor_state` returns `playMode: true/false`.
- **UI bars: `localScale.x` or `sizeDelta.x`** — never `Image.fillAmount` without sprite.
- **Input System throughout** — Input System actions for gameplay, `InputSystemUIInputModule` on EventSystem for UI. Never legacy `StandaloneInputModule` or `Input.GetKey`.
- **animator.speed = 0** when using FrameRateSampler — never advance time via Update otherwise.

## Sub-Agent Team (for complex multi-agent tasks only)

| Agent | Department | Pattern File |
|-------|-----------|-------------|
| `gameplay-engineer` | Gameplay Engineering | `gameplay.md` |
| `animation-engineer` | Animation Engineering | `animation.md` |
| `systems-engineer` | Systems Engineering | `systems.md` |
| `ui-engineer` | UI Engineering | `ui.md` |
| `tools-engineer` | Tools Engineering | `tools.md` |
| `qa-engineer` | QA Engineering | `qa.md` |

When spawning a specialist: "Read `.claude/teams/game-dev/{domain}.md` for patterns and `Assets/Docs/ProjectKnow/{domain}.md` for project specifics."

## Learning Protocol

During sessions, when you discover something worth keeping:
1. **Pattern** (applies to any Unity project) → propose `.claude/teams/game-dev/{domain}.md`
2. **Project** (this prototype only) → propose `Assets/Docs/ProjectKnow/{domain}.md`
3. **Principle-level mistake** → propose project memory (`memory/feedback_*.md`)
4. **Always ask before writing.** Never save silently.
