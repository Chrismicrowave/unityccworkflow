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
