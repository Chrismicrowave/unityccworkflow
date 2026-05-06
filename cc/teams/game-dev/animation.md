# Game Dev — Animation Patterns

General Unity animation patterns reusable across projects.

## Spider-Verse Frame Rate Technique

Freeze Unity's automatic animator pass and advance manually at a target FPS to create a stepped/choppy look while keeping 1x playback speed.

```csharp
// Core pattern
void Awake() { _animator = GetComponent<Animator>(); }
void Start() { _animator.Update(0f); _animator.speed = 0f; }
void Update() {
    float interval = 1f / targetFPS;
    _elapsed += Time.deltaTime;
    if (_elapsed >= interval) {
        float step = _elapsed;
        _elapsed = 0f;
        _animator.speed = 1f;     // must be > 0 for manual Update to work
        _animator.Update(step);    // advance by real elapsed time = 1x speed
        _animator.speed = 0f;     // re-freeze before automatic pass
    }
}
```

Key details:
- `animator.speed = 0` between steps holds the last pose
- **Unity 6 quirk**: `_animator.Update(step)` is silently ignored when speed=0. Must temporarily set speed=1 during the call
- `if` not `while` — skip a frame if lagging, don't try to catch up (intentional choppy look)
- Works with Apply Root Motion: delta position/rotation is computed during `_animator.Update()`
- Each step advances by the wall-clock time elapsed (`step = _elapsed`), so playback stays at exactly 1x speed regardless of target FPS

## Mixamo FBX Import

Correct workflow for Mixamo animations imported as Humanoid:

1. Set Rig to **Humanoid**, assign the **same Avatar** as your character
2. Import settings:
   - **Loop Time** — tick for looping clips (idle, walk, run)
   - **Root Transform Rotation** → **Original**
   - **Root Transform Position Y** → **Original** (use **Offset** field to fine-tune height to match mesh)
   - **Root Transform Position XZ** → **Original**
   - **Bake Into Pose** → tick for **Root Transform Y** (prevents character sinking during actions)
3. Enable **Apply Root Motion** on the character's Animator component
4. Extract standalone `.anim` files after setting these import settings (the clip data bakes in the import settings at extraction time)

**Why:** This eliminates character sinking during action animations (cartwheels, breakdance, kicks) without scripted RootT.y curve offsets. The offset field in Root Transform Position Y adjusts the animation's vertical position to match the specific model mesh.

## Detecting Animation End

When using manual `_animator.Update()` (speed=0 technique), polling `normalizedTime` works for detecting clip completion:

```csharp
IEnumerator WaitForActionEnd() {
    var state = _animator.GetCurrentAnimatorStateInfo(0);
    while (state.normalizedTime < 1f) {
        yield return null;
        state = _animator.GetCurrentAnimatorStateInfo(0);
    }
}
```

This self-corrects: `normalizedTime` only advances during manual `_animator.Update()` calls, so the poll naturally waits the right amount of wall-clock time regardless of target FPS.

## RootT.y Curve Binding

Root transform curves on a Humanoid animator are bound to the `Animator` component, not `Transform`:

```csharp
var binding = new EditorCurveBinding {
    path = "",
    type = typeof(Animator),
    propertyName = "RootT.y"  // or RootT.x, RootT.z, RootQ.x, etc.
};
var curve = AnimationUtility.GetEditorCurve(clip, binding);
```

This is for editor scripts that need to read or modify root motion curves programmatically.

## Per-State FPS Override Pattern

When using the frame-rate technique, you can vary the target FPS per-animator-state by checking `shortNameHash`:

```csharp
float GetTargetFPS() {
    int hash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
    if (hash == actionState && overrideAction) return actionFPS;
    return defaultFPS;
}
```

Useful for making action animations read at a different rate than locomotion.
