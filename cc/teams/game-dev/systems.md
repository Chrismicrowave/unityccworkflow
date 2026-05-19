# Systems Patterns

## Script controls initial active state

Objects that must be active/inactive when the game starts should be controlled by script (`SetActive` in `Awake`/`Start`), not by the GameObject's active toggle in the scene hierarchy. This lets you toggle objects on/off in-editor while working without worrying about breaking the game's initial state.

**Why:** Scene assets are shared between the authoring workflow and runtime. An object you deactivate "for the game" (e.g., GameOver panel) also disappears from the editor Hierarchy when you're trying to work on it. Keeping everything active in-scene and letting scripts handle initial state decouples editing from gameplay.

**How to apply:** When adding a UI panel or object that should only appear at a specific game event:
1. Leave the GameObject **active** in the scene
2. In `Awake()`, call `gameObject.SetActive(false)` or `panel.SetActive(false)`
3. When the event triggers, call `SetActive(true)` again
