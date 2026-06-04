# Systems Patterns

## Two approaches to shared references

### Approach A: Hardwired RefHub (preferred)

Wire all shared-system references via `[SerializeField]` at edit time. No runtime registration.

**RefHub** holds serialized references:
```csharp
public class RefHub : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] PlayerController _playerController;
    public PlayerController PlayerController => _playerController;
    // ...
}
```

**Each consumer** accepts its own serialized RefHub:
```csharp
public class SomeConsumer : MonoBehaviour
{
    [SerializeField] RefHub _refHub;

    void Start()
    {
        // Safe at any lifecycle point — reference was set at edit time, not resolved at runtime
        var pc = _refHub.PlayerController;
    }
}
```

**Benefits:**
- Zero race conditions — references are serialized, not resolved at runtime
- No startup-order dependency — Awake vs Start ordering irrelevant for RefHub access
- Explicit — every dependency visible in the inspector
- No silent nulls — unwired slots are trivially visible

**Trade-off:** requires inspector wiring for every consumer.

### Approach B: Self-registration (not recommended)

Systems register themselves in Awake, consumers resolve in Start:
```csharp
void Awake() { RefHub.Instance.PlayerController = this; }
void Start() { var pc = RefHub.Instance.PlayerController; }
```

**Trade-off:** simpler initial setup, but introduces startup-order dependencies. The `??` operator silently ignores Unity fake-null. Only viable for small projects.

## No name-based lookups

**Never use `FindObjectOfType`, `FindGameObjectWithTag`, `GameObject.Find`, or named `Get<T>(string)` / `Register(string, ...)` patterns in runtime code.**

These create hidden dependencies that silently break when tags change, objects are renamed, or scene structure shifts:

| Method | Problem |
|--------|---------|
| `FindObjectOfType<T>()` | Slow (scans all objects), returns arbitrary instance if multiples exist |
| `FindGameObjectWithTag` | Silent null if tag is wrong or object is inactive |
| `GameObject.Find("name")` | Silent null if renamed or reparented |
| `GetComponent<T>("name")` / `Register("name", ...)` | No compile-time checking, string mismatch bugs |

**Instead:**
- **RefHub direct properties** for shared systems — every dependency visible in the inspector
- **`[SerializeField]` drag-and-drop** for per-consumer references
- **Tag/type-based lookup at specific points** only when the object genuinely can't be known at edit time (e.g., runtime-spawned entities) — and even then, consider a registry pattern

**If you have a solid reason to use a name-based lookup** (e.g., truly dynamic objects), propose it to the user first with the specific rationale.

## Input Action Asset debugging

**When a binding doesn't fire, check the raw JSON for HTML-escaped paths.**

MCP tools (`add_action`, `add_bindings`, `add_composite_binding`) can silently corrupt binding paths with HTML entities:

- `&lt;Keyboard&gt;/f` instead of `<Keyboard>/f`
- `&lt;Mouse&gt;/leftButton` instead of `<Mouse>/leftButton`

Unity's Input System silently ignores HTML-escaped paths — the action never fires and no error is logged.

**Check command:**
```bash
grep -E '&lt;|&gt;' Assets/InputSystem_Actions.inputactions
```

**Fix:** Replace `&lt;` with `<` and `&gt;` with `>` in the path field. Also ensure `groups` is set (e.g. `"Keyboard&Mouse"`) so the binding matches the active control scheme.

- **Awake** is for yourself: `GetComponent`, `GetComponentsInChildren`, setting private fields.
- **Start** is for talking to others: runtime lookups (`Camera.main`, `FindObjectOfType`), subscribing to other scripts' events.
- **Why**: Unity runs all Awakes in undefined order, then all Starts in undefined order. Reading cross-script state in Awake is a race condition. Start guarantees every script has had its Awake.
- **Hardwiring note**: Inspector-wired `[SerializeField]` references are set at edit time. Reading a serialized `_refHub.PlayerController` is safe in Awake, Start, or any other method — no ordering dependency.
- **Exception**: none. Execution-order settings are a fragile workaround, not a fix.

## Subscribe in Start, not OnEnable

- **OnEnable is for your own setup**, not for subscribing to other scripts' events.
- **When you subscribe to a singleton event in OnEnable**: the singleton's `Awake()` (which sets `Instance = this`) may not have run yet, especially if both components are on the same GameObject and the singleton is listed later in the inspector order. The subscription silently fails — no error, no warning, the event never fires.
- **Start guarantees all Awakes have completed**, so the singleton Instance is guaranteed to exist.
- **Common violation**: `GameModeManager.Instance.OnModeChanged += Handler` in `OnEnable()`. If GameModeManager is the last component on the GO, its Awake hasn't run yet — `Instance` is null, subscription silently dropped.
- **Rule**: subscribe in `Start()`, unsubscribe in `OnDisable()`. This survives disable/re-enable cycles and guarantees the provider is initialized.
- **Exception**: subscribing to self-resolved events (e.g. `_myAction.performed += Handler` where `_myAction` was resolved from your own `GetComponent<PlayerInput>()` in Awake) — fine in OnEnable since the resolution doesn't depend on other scripts.

## Canvas↔world coordinate conversion

**When converting between canvas UI coordinates and 3D world/viewport space, never pass `GetWorldCorners` values directly through a camera's `WorldToViewportPoint`/`ViewportToWorldPoint`.**

Canvas positions (especially ScreenSpace-Overlay mode) live in a different coordinate frame than 3D world space. `GetWorldCorners` returns pixel positions mapped to world units at z=0 — passing these through a 3D camera's projection methods produces garbage viewport coordinates because the camera treats them as 3D positions at its near clip plane.

**Always use `RectTransformUtility.WorldToScreenPoint`** to convert UI corners to screen pixels first, then divide by `Screen.width`/`Screen.height` for viewport coordinates:

```csharp
Camera canvasCam = null;
var canvas = rectTransform.GetComponentInParent<Canvas>();
if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
    canvasCam = canvas.worldCamera;

Vector3[] corners = new Vector3[4];
rectTransform.GetWorldCorners(corners);

Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(canvasCam, corners[0]);
Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(canvasCam, corners[2]);

Vector2 vpMin = new Vector2(
    Mathf.Min(screenBL.x, screenTR.x) / Screen.width,
    Mathf.Min(screenBL.y, screenTR.y) / Screen.height
);
Vector2 vpMax = new Vector2(
    Mathf.Max(screenBL.x, screenTR.x) / Screen.width,
    Mathf.Max(screenBL.y, screenTR.y) / Screen.height
);
```

## Editor values beat script defaults

For any `public` or `[SerializeField] private` variable, always check the Unity Editor inspector value. Inspector-configured values are authoritative; script defaults are just fallbacks.

**Why:** Serialized fields in prefabs and scenes override script defaults at edit time. The value shown in the inspector is what actually runs — the script default only applies when a field has never been set in the inspector. Trusting `= 5` in code when the inspector shows `10` is a runtime bug.

**Pattern:**
1. After adding/changing a serialized field, verify its value via `get_game_object_info` or `Read` on the prefab asset
2. When in doubt, prefer a nonzero/meaningful inspector default over leaving it as `0`/`null` — you'll catch unwired slots faster
3. Remember `[Range(0, 1)]` and `Header` attributes don't affect serialization — they only change inspector display

## Always use the new Input System

Use `InputActionAsset` with generated C# wrapper classes. Never poll `Keyboard.current`/`Mouse.current` directly or use legacy `Input.GetKey`.

**Prefer generated wrapper class events** over direct `.inputactions` asset lookups:

```csharp
// ✅ Good — generated wrapper
var actions = new InputSystem_Actions();
actions.Player.Jump.performed += OnJump;

// ❌ Bad — manual asset path lookup (stringly-typed, no compile check)
var asset = Resources.Load<InputActionAsset>("InputSystem_Actions");
asset.FindActionMap("Player").FindAction("Jump").performed += OnJump;

// ❌ Worse — legacy input
if (Input.GetKeyDown(KeyCode.Space)) Jump();
```

**When bindings don't work:** Check the raw `.inputactions` JSON for HTML-escaped paths (`&lt;Keyboard&gt;` instead of `<Keyboard>`) — see "Input Action Asset debugging" section above.

## Never `??` with Unity Objects

**Always use `if (x == null)` for Unity Objects, never `??` or `?.`.**

Unity overrides the `==` operator on all `UnityEngine.Object` subclasses (GameObject, MonoBehaviour, ScriptableObject, etc.) to implement "fake null." When an object is destroyed, Unity's `==` returns true but the C# managed object still exists — `??` uses C# reference equality and returns the "dead" object reference instead of the fallback.

```csharp
// ✅ Correct
if (_target == null) _target = FindFirstObjectByType<MyType>();

// ❌ Bug — fake null bypasses ??
_target ??= FindFirstObjectByType<MyType>();

// ✅ Correct — avoid ?. when you need null-aware access
if (_target != null) _target.DoSomething();

// ❌ Bug — ?. works on the live C# ref which isn't null
_target?.DoSomething();  // executes even on destroyed objects
```

**Rule of thumb:** If a type inherits from `UnityEngine.Object`, always use `== null`. C# null-conditional operators (`?.`, `??`, `??=`) are only safe on plain C# objects, interface types on MonoBehaviours (cast to interface loses Unity's custom equality), or nullables.

## Script controls initial active state

Objects that must be active/inactive when the game starts should be set via script (`Awake`/`Start`), not by the GameObject's active toggle in the scene.

**Why:**
- You toggle GOs on/off while working in the editor (to see behind them, isolate objects, etc.)
- If the initial state is baked into the scene's active toggle, those editor toggles become permanent — save the scene and you've lost the intended start state
- A script in `Awake()` or `Start()` resets the state reliably at runtime regardless of what you did in the editor

```csharp
void Awake()
{
    gameObject.SetActive(false);  // starts inactive at runtime, but you can toggle in-editor freely
}
```

**Exception:** Objects that are purely decorative or never need to be toggled in-editor. Even then, the script approach is zero-cost and future-proof.
