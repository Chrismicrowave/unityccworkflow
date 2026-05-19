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
