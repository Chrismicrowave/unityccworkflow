# MonoBehaviour Lifecycle

## Execution Order (per Unity official docs)

### Initialization (Scene Load / Object Creation)
```
Scene Load / Instantiate
  |
  v
Awake (all GameObjects, non-deterministic order between objects)
  |
  v
OnEnable (all GameObjects, after Awake completes)
  |
  v
sceneLoaded event raised (after OnEnable, before Start)
  |
  v
Start (called before first frame update, only if component is enabled)
```

### Per-Frame Update Loop
```
FixedUpdate  (0, 1, or multiple times per frame -- physics timestep)
  |
  v
Update  (once per frame, all GameObjects)
  |
  v
Animation calculations (internal)
  |
  v
LateUpdate  (once per frame, after all Updates and animations)
```

### Rendering Loop (Built-in Render Pipeline Only)
```
OnPreCull
OnBecameVisible / OnBecameInvisible
OnWillRenderObject
OnPreRender
OnRenderObject
OnPostRender
OnRenderImage
OnGUI  (multiple times per frame for IMGUI events)
OnDrawGizmos
```

### Deactivation / Destruction
```
OnDisable  (component disabled, parent deactivated, or during destruction)
  |
  v
OnDestroy  (object or component about to be destroyed)
```

### Application Lifecycle
```
OnApplicationPause  (app loses/regains focus on mobile)
OnApplicationFocus  (player gains/loses focus)
OnApplicationQuit   (sent to all active GameObjects before quit)
```

### Animation-Specific Callbacks (within Animation Update)
```
OnAnimatorMove  (process root motion)
OnAnimatorIK    (process Inverse Kinematics)
```

### Physics Callbacks (fire as events throughout the frame)
```
OnCollisionEnter / OnCollisionStay / OnCollisionExit
OnTriggerEnter / OnTriggerStay / OnTriggerExit
OnJointBreak
```

---

## Key Rules

### Rule 1: Awake runs regardless of enabled state
Awake executes even if `Behaviour.enabled = false`. The only prerequisite is that the GameObject itself is (or becomes) active in the hierarchy. This makes Awake the safest place for self-contained initialization (getting components, assigning references).

### Rule 2: All Awake calls finish before any Start call
Unity guarantees that every active GameObject in the scene receives its Awake callback before any GameObject receives Start. This is the fundamental design contract that enables the Awake/Start split: use Awake for self-setup, use Start for cross-object communication.

### Rule 3: OnEnable fires every time the component becomes active
OnEnable is not a one-time event. It fires:
- On first activation (after Awake, before Start)
- Every time the component is toggled from disabled to enabled
- Every time the GameObject (or a parent) transitions from inactive to active
This makes OnEnable the correct place for subscribing to events, registering with managers, and allocating from pools -- with the corresponding cleanup in OnDisable.

### Rule 4: Start runs exactly once per lifetime
Unlike OnEnable, Start will only fire once no matter how many times the component is toggled off and on. If the component was disabled at initialization, Start is deferred until the component first becomes enabled.

### Rule 5: FixedUpdate and Update are decoupled
FixedUpdate runs at a fixed timestep (`Time.fixedDeltaTime`, default 0.02s). Update runs once per rendered frame. FixedUpdate may be called zero, one, or multiple times per frame depending on rendering performance. Physics operations must go in FixedUpdate for deterministic results.

### Rule 6: LateUpdate runs after ALL Updates
LateUpdate fires after every MonoBehaviour's Update has been called for that frame, and after animation calculations. This makes it the correct place for camera-follow logic and any "after all movements are resolved" code.

### Rule 7: OnDisable fires during destruction and scene unload
OnDisable is called not only when the component is manually disabled, but also:
- During `Object.Destroy()` (before OnDestroy)
- During scene unload
- During domain reload (in Editor with default settings)
This makes OnDisable the reliable cleanup counterpart to OnEnable. Unsubscribe events here.

### Rule 8: OnDestroy only fires on previously-active GameObjects
If a GameObject was never active, OnDestroy will not be called. On mobile, the OS may terminate the app without calling OnDestroy -- never rely on it for saving critical state.

### Rule 9: Disabling a component does NOT stop its coroutines
Setting `enabled = false` stops Update, FixedUpdate, LateUpdate, and OnGUI from being called, but coroutines started via `StartCoroutine()` continue running. To stop coroutines, call `StopCoroutine()`/`StopAllCoroutines()` or deactivate the GameObject.

### Rule 10: No inter-GameObject ordering for the same event
Unity does not guarantee the order in which the same event (e.g., Update) is called across different GameObjects, unless configured via Script Execution Order settings. Parent/child relationships do not guarantee ordering.

### Rule 11: Instantiate calls Awake and OnEnable synchronously
When you call `Object.Instantiate()`, Awake and OnEnable are called during the Instantiate call itself (for objects active in the hierarchy). Start is deferred until before the first Update.

### Rule 12: Domain reload affects lifecycle in Editor
When domain reload is disabled (fast Enter Play Mode):
- Static variables persist across Play mode sessions
- Static events keep registered subscribers
- OnDisable/OnEnable are skipped for `[ExecuteAlways]` scripts
- You must manually reset state using `[RuntimeInitializeOnLoadMethod]` or `EditorApplication.playModeStateChanged`

---

## Common Pitfalls

### Pitfall 1: Assuming Awake order between objects
```csharp
// BAD: Manager might not have initialized yet
void Awake() {
    Manager.Instance.Register(this); // Manager.Awake may not have run yet
}
```
Fix: Use Start for cross-object communication -- all Awake calls complete before any Start.

### Pitfall 2: Subscribing in Start, unsubscribing in OnDisable
```csharp
void Start() { SomeEvent += Handler; }
void OnDisable() { SomeEvent -= Handler; }
```
Problem: Start runs once, OnEnable/OnDisable can cycle. After toggle off/on, OnEnable fires but Start does not -- subscription is lost but OnDisable still tries to unsubscribe next cycle.
Fix: Pair OnEnable (subscribe) with OnDisable (unsubscribe), or Start (subscribe) with OnDestroy (unsubscribe).

### Pitfall 3: Physics operations in Update
Physics forces/velocities/Rigidbody changes belong in FixedUpdate. Update runs at frame rate, not physics timestep, producing inconsistent results.

### Pitfall 4: Forgetting OnDestroy does not always run
On mobile, OnDestroy may not fire if the OS terminates the app. Save critical state in OnApplicationFocus or OnApplicationPause.

### Pitfall 5: Relying on enabled = false to stop coroutines
```csharp
enabled = false; // Update stops, but coroutines keep running!
```
Fix: Explicitly stop coroutines in OnDisable, or deactivate the GameObject.

### Pitfall 6: Setting enabled = false in Awake
OnEnable has already been queued when Awake runs -- setting enabled = false in Awake does NOT prevent OnEnable from firing. It WILL prevent Start from running (deferred until re-enable).

### Pitfall 7: Coroutines in OnEnable without cleanup in OnDisable
```csharp
void OnEnable() { StartCoroutine(MyLoop()); }
void OnDisable() { /* forgot to stop */ }
```
Disabling the component does not stop coroutines. Toggle off/on starts a duplicate. Fix: StopCoroutine in OnDisable.

### Pitfall 8: Empty OnGUI
Even `void OnGUI() {}` adds IMGUI processing overhead every frame. Use uGUI/UI Toolkit instead.

---

## Best Practices

### 1. Use Awake for self-contained setup only
```csharp
void Awake() {
    _rigidbody = GetComponent<Rigidbody>();
    _animator = GetComponentInChildren<Animator>();
    _collider = GetComponent<Collider>();
}
```
Do NOT register with managers, access other objects, or rely on external state in Awake.

### 2. Use Start for cross-object communication
```csharp
void Start() {
    // Safe: all Awake calls have completed
    Manager.Instance.Register(this);
}
```

### 3. Pair OnEnable with OnDisable for subscriptions and pooling
```csharp
void OnEnable() {
    EventManager.OnPlayerDeath += HandleDeath;
    _pooledEffect.SetActive(true);
    _damageCoroutine = StartCoroutine(DealDamageOverTime());
}

void OnDisable() {
    EventManager.OnPlayerDeath -= HandleDeath;
    _pooledEffect.SetActive(false);
    if (_damageCoroutine != null) {
        StopCoroutine(_damageCoroutine);
        _damageCoroutine = null;
    }
}
```
Correctly handles multiple enable/disable cycles.

### 4. Pair Start with OnDestroy for permanent subscriptions
```csharp
void Start() {
    SaveManager.OnGameSaved += HandleGameSaved;
}
void OnDestroy() {
    SaveManager.OnGameSaved -= HandleGameSaved;
}
```
Use when the subscription should last the entire object lifetime.

### 5. Camera follow in LateUpdate
```csharp
void LateUpdate() {
    transform.position = Vector3.Lerp(
        transform.position, _target.position + _offset, Time.deltaTime * _smoothSpeed
    );
}
```
Target finishes all movement in Update before camera adjusts.

### 6. Physics in FixedUpdate
```csharp
void FixedUpdate() {
    _rigidbody.AddForce(_moveDirection * _speed, ForceMode.Acceleration);
}
```

### 7. Use Script Execution Order sparingly
In Project Settings > Script Execution Order, set execution order for scripts that MUST run in a specific sequence. Overusing creates hidden dependencies. Default Time is fine for most scripts.

### 8. Cache component references in Awake
Component serialized state is undefined at construction time. Awake is the first safe point for GetComponent results.

### 9. Use OnBecameVisible/OnBecameInvisible for culling
```csharp
void OnBecameVisible() { enabled = true; }
void OnBecameInvisible() { enabled = false; }
```
Stops Update/FixedUpdate/LateUpdate when off-screen.

### 10. Handle domain reload with fast Enter Play Mode
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
static void ResetStatics() { _instanceCount = 0; }
```

### 11. Be deliberate about enabled = false in lifecycle methods
- In **Awake**: OnEnable still fires (already queued), Start is deferred.
- In **OnEnable**: Start is prevented (if not yet called), Update prevented.
- In **Start**: Update/LateUpdate prevented starting next frame.

### 12. Coroutine yield resume timing
- `yield return null` → resumes next frame (during Update)
- `yield return new WaitForFixedUpdate()` → after next FixedUpdate
- `yield return new WaitForEndOfFrame()` → after rendering
- `yield return new WaitForSeconds(t)` → after t seconds game time

---

## Source References

- Unity Manual -- Order of Execution for Event Functions: https://docs.unity3d.com/Manual/execution-order.html
- Unity Manual -- Event Functions: https://docs.unity3d.com/Manual/event-functions.html
- Unity Scripting API -- MonoBehaviour: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
- Unity Manual -- Coroutines: https://docs.unity3d.com/Manual/Coroutines.html
- Unity Manual -- Domain Reloading: https://docs.unity3d.com/Manual/domain-reloading.html
