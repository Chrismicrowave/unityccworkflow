# Game Dev — Cinemachine Patterns

Cinemachine API quirks and integration patterns for Unity 6 (Cinemachine 3).

## Follow/LookAt in Cinemachine 3 (Unity 6)

In Cinemachine 3, `Follow` and `LookAt` are **C# properties**, **not `SerializedProperty` fields**. This has important implications:

### What Doesn't Work

```csharp
// DOES NOT WORK in Cinemachine 3 — returns null
var so = new SerializedObject(cinemachineCamera);
var followProp = so.FindProperty("m_FollowTarget");   // null
var lookAtProp = so.FindProperty("m_LookAtTarget");   // null
```

The old serialized field names from Cinemachine 2 (`m_FollowTarget`, `m_LookAtTarget`, `m_Follow`, etc.) do not exist in Cinemachine 3. Internally, Follow/LookAt are backed through `Target.TrackingTarget` and `Target.CustomLookAtTarget`/`Target.LookAtTarget`.

### What Works

Direct C# property assignment:

```csharp
var c = cinemachineCamera;
c.Follow = targetTransform;   // direct C# property — works
c.LookAt = targetTransform;   // direct C# property — works
```

### Persisting Changes via Editor Scripts

When modifying Cinemachine camera Follow/LookAt references from an editor script or `execute_script`, the scene must be saved explicitly:

```csharp
// Step 1: Assign via direct C# property
var c = Resources.FindObjectsOfTypeAll<CinemachineCamera>()
    .FirstOrDefault(cam => cam.name == "CM_FollowCam");
c.Follow = camLookAt;
c.LookAt = camLookAt;

// Step 2: Mark dirty and save scene
EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
EditorSceneManager.SaveOpenScenes();
```

`AssetDatabase.SaveAssets()` is not sufficient — it only saves assets, not scenes. You must use `EditorSceneManager`.

### MCP tool limitations

- `set_property` MCP tool for Follow/LookAt GameObject references on Cinemachine cameras may report success but not persist to the scene.
- For reliable programmatic changes to Cinemachine cameras, use `execute_script` with direct C# property assignment + `EditorSceneManager.SaveOpenScenes()`.

## Finding Cinemachine Cameras

- **`Resources.FindObjectsOfTypeAll<CinemachineCamera>()`** — finds cameras even when they are inactive (not enabled). Useful when searching for cameras that are toggled on/off by mode systems.
- `GameObject.Find(name)` — searches by **name only**, not hierarchical path. `GameObject.Find("Player/CM_FollowCam")` returns null even if the object exists. Use `GameObject.Find("CM_FollowCam")` instead.

## Cinemachine Priority for Camera Takeover

When switching between Cinemachine cameras (e.g., from a follow cam to a paint cam), use priority to control which camera is active:

```csharp
// Boost paint cam priority above follow cam
paintCinemachineCam.Priority = 60;   // follow cam is typically 10
```

No need to deactivate the other camera — Cinemachine's priority system handles blending automatically. To prevent the follow cam from competing, set follow cam priority back when exiting paint mode (though deactivating the paint cam GO is sufficient since inactive cameras are not blended).

## DefaultBlend Timing — CM3 Re-Reads During Active Blends

**Critical CM3 behavior:** Unlike Cinemachine 2, CM3 does **not** capture `DefaultBlend` at the moment a blend starts. It re-reads `CinemachineBrain.DefaultBlend` during an active blend's lifetime. This means:

```csharp
// WRONG — restoring DefaultBlend mid-transition corrupts the in-progress blend
brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
ChangeCameraPriority();  // brain starts blend with Cut
brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Linear, 0.2f); // ← CM3 re-reads this!
// The in-progress Cut blend instantly becomes Linear
```

### Safe Pattern: Keep Override Until Transition Completes

Cache the inspector default at `Start()`, override it, and only restore after the blend finishes:

```csharp
CinemachineBlendDefinition _savedDefaultBlend;

void Start()
{
    var brain = Camera.main?.GetComponent<CinemachineBrain>();
    if (brain != null)
        _savedDefaultBlend = brain.DefaultBlend;
}

public void EnterWithCut()
{
    var brain = Camera.main?.GetComponent<CinemachineBrain>();
    if (brain != null)
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);

    // Must happen in a frame where the brain's LateUpdate will see Cut
    ChangeCameraPriority();
}

public void ExitAndRestore()
{
    var brain = Camera.main?.GetComponent<CinemachineBrain>();
    if (brain != null)
        brain.DefaultBlend = _savedDefaultBlend;
    RestoreCameraPriority();
}
```

The safest variant: **keep the override active for the entire mode/session** and only restore on exit. `DefaultBlend` only matters on frames where a new blend starts — having it set to Cut while the FP camera is already active is harmless.

### Timing: Brain Processes in LateUpdate

`CinemachineBrain` processes blends in `LateUpdate` (UpdateMethod defaults to SmartUpdate → LateUpdate). Setting `DefaultBlend` and changing priorities in the same frame means the brain processes both in that frame's LateUpdate.

If you need to check `brain.ActiveBlend` to wait for completion, note that it will be `null` on Cut transitions (Duration=0) before the brain's LateUpdate has run. Always `yield return null` after the priority change to give the brain a frame to process before checking.

### References
- `CinemachineBrain.DefaultBlend` — struct field `CinemachineBlendDefinition` with `Style` (enum) and `Time` (seconds)
- `CinemachineBlendDefinition.Styles` — `Cut=0, EaseInOut=1, EaseIn=2, EaseOut=3, HardIn=4, HardOut=5, Linear=6, Custom=7`
- Inspector-configured DefaultBlend overrides the script default (EaseInOut, 2f) — always check the scene's Main Camera brain inspector
- `brain.ActiveBlend` — `CinemachineBlend` or null; `IsValid` property checks if CamA/CamB are valid
