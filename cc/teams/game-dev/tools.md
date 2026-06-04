# Game Dev — Tools Patterns

General Unity editor tool patterns reusable across projects.

## Wire references via MCP, never ask

When you add a new serialized field (`public` / `[SerializeField]`) to a MonoBehaviour in the scene, wire it immediately via the `set_property` MCP tool. **Never tell the user to do it manually.**

**Pattern:**
1. Add the field in code
2. Wait for Unity to compile (compile-stop hook fires)
3. Use `get_game_object_info` to find the target component's hierarchy path
4. Call `set_property` to assign the reference immediately

**Exceptions** (these genuinely require manual wiring):
- Asset references (materials, prefabs, ScriptableObjects) — must be dragged from the Project window
- Targets that can't be resolved by hierarchy path (e.g., runtime-only objects)
- User explicitly says "I'll wire it"

**Why:**
- Every serialized null slot is a runtime bug waiting to happen
- The user shouldn't have to open the inspector to fix something the AI just wrote
- Wire-it-yourself means the feature works on the very first Play Mode test

## Extracting Animation Clips from FBX

Use `EditorUtility.CopySerialized` to copy named clips from the FBX sub-assets into standalone `.anim` files:

```csharp
// Example pattern — load FBX, copy clip, save as .anim
string[] clipNames = { "Breathing Idle", "Walking", ... };
foreach (var name in clipNames) {
    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fbxPath);
    // Access sub-assets by name
    var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath);
    var source = subAssets.FirstOrDefault(a => a.name == name) as AnimationClip;
    var copy = Object.Instantiate(source);
    AssetDatabase.CreateAsset(copy, $"Assets/Anim/{name}.anim");
}
```

After re-importing the FBX with changed settings, re-extract — the `.anim` files are snapshots of the import's baked data.

## EditorUtility.CopySerialized Pattern

For duplicating any Unity asset programmatically:

```csharp
var copy = ScriptableObject.CreateInstance<MyAssetType>();
EditorUtility.CopySerialized(source, copy);
AssetDatabase.CreateAsset(copy, path);
```

`CopySerialized` copies all serialized fields from source to destination. Works on AnimationClip, Material, ScriptableObject, and any `UnityEngine.Object`.

## Trigger Input Actions Via Script, Not Simulated Keypresses

When testing input-triggered functions in Play Mode, call the C# method directly via MCP `execute_script` instead of simulating key presses (SendKeys, PowerShell input injection, etc.). Simulated keypresses suffer from focus issues, timing problems, and don't work in batch/headless contexts.

**Pattern:**
1. Create a `[PublicAPI]` or editor-only static method in the target class (or a separate debug class)
2. Call it via `execute_script` in the MCP session
3. Read Unity console logs to verify the result

```csharp
// Assets/Scripts/Editor/DebugThing.cs
public static class DebugThing
{
    public static void Execute()
    {
        var mgr = Object.FindFirstObjectByType<SomeManager>();
        mgr.DoSomething();
    }
}
```

```csharp
// Or call a method directly on a component
var camMgr = Object.FindFirstObjectByType<CameraManager>();
if (camMgr != null) camMgr.ToggleFirstPersonWithCut();
```

**Caveat:** The `execute_script` call runs synchronously in the editor — it won't see results from coroutines/yield instructions that span frames. For frame-spanning operations, add debug logs in the coroutine and check the console afterward.

## Diagnostic Instrumentation for Input/Action Pipelines

When debugging a multi-layer input pipeline (key press → dispatch → handler → result), add layered `Debug.Log` at every boundary *before* trying to fix. This makes any single Play Mode test pinpoint exactly which link is broken:

```csharp
// Layer 1: Input handler (e.g., PlayerController)
void OnToggleFirstPerson(InputAction.CallbackContext ctx)
{
    Debug.Log($"[PlayerController] Q pressed, CameraManager ref: {_refHub?.CameraManager != null}");
    _refHub?.CameraManager.ToggleFirstPersonWithCut();
}

// Layer 2: Dispatch bridge (e.g., CameraManager)
public void ToggleFirstPersonWithCut()
{
    var brain = Camera.main?.GetComponent<CinemachineBrain>();
    Debug.Log($"[CameraManager] ToggleFirstPersonWithCut — _firstPerson={_firstPerson}, brain={brain != null}");
    // ...
}

// Layer 3: Action handler (e.g., FPSCamMode)
void OnEnable()
{
    Debug.Log($"[FPSCamMode] OnEnable — _cam={_cam != null}, _attackAction subscribed={_subscribed}");
    _attackAction.performed += OnTakePhoto;
    _subscribed = true;
}
```

**Pattern:**
1. Instrument every method in the call chain with a unique prefix `[ClassName]` for grep filtering
2. Log reference resolution state (null checks, what resolved) — not just "entered method"
3. Log branch decisions (which `if` was taken, why)
4. Run one Play Mode test, check the console in one pass
5. Remove all instrumentation in a cleanup commit once the bug is found

The instrumentation is disposable — commit it separately so the fix commit is clean.
`git diff` or checking Unity's console makes the before/after obvious.

## Adding FullScreenPassRendererFeature Programmatically

For editor setup scripts that configure a renderer with a fullscreen effect:

```csharp
var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
var serializedData = new SerializedObject(rendererData);
var featuresProp = serializedData.FindProperty("m_RendererFeatures");

// Check if feature already exists
for (int i = 0; i < featuresProp.arraySize; i++)
{
    var feature = featuresProp.GetArrayElementAtIndex(i).objectReferenceValue;
    if (feature != null && feature.name == "MyFeature")
    {
        // Update existing feature
        var fsFeature = feature as FullScreenPassRendererFeature;
        fsFeature.passMaterial = material;
        EditorUtility.SetDirty(fsFeature);
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        return;
    }
}

// Create new feature
var newFeature = ScriptableRendererFeature.CreateInstance<FullScreenPassRendererFeature>();
newFeature.name = "MyFeature";
newFeature.passMaterial = material;
newFeature.passIndex = 0;
newFeature.fetchColorBuffer = true;          // populates _BlitTexture
newFeature.requirements = ScriptableRenderPassInput.Normal;  // depth + normals
newFeature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;

AssetDatabase.AddObjectToAsset(newFeature, rendererData);
featuresProp.arraySize++;
featuresProp.GetArrayElementAtIndex(featuresProp.arraySize - 1).objectReferenceValue = newFeature;
serializedData.ApplyModifiedProperties();
EditorUtility.SetDirty(rendererData);
AssetDatabase.SaveAssets();
```

Key points:
- Use `SerializedObject` to modify the renderer data asset (RendererFeature list is not directly accessible)
- Check for existing feature by name before adding to avoid duplicates
- `AddObjectToAsset` makes the feature a sub-asset of the renderer data
- `fetchColorBuffer` must be true for the shader to receive `_BlitTexture`
- `requirements = Normal` requests depth + normal textures from the pipeline

## Stop Play mode before scene edits

Changes made during Play Mode are discarded when Play Mode stops. Always check the Play Mode state before calling MCP tools that modify scene objects.

**Pattern:**
1. Call `get_unity_editor_state` and check the `playMode` field
2. If `playMode` is true, call `stop_game` first
3. Then proceed with scene edits

**Why:** Unity discards all runtime modifications when exiting Play Mode. If you add a component, move a GameObject, or change a serialized property during Play Mode, it's gone the moment the user stops the game. This includes MCP tool calls — `add_component`, `set_property`, `set_transform`, etc. all write to the scene's in-memory state which doesn't persist past Play Mode.

## Save scene before MCP hierarchy search

Before calling MCP tools that search the scene hierarchy (`list_game_objects_in_hierarchy`, `get_game_object_info`), save the scene first. Unsaved edits don't exist on disk and won't be found by hierarchy searches.

**Why:** MCP tools that query the scene hierarchy read from the serialized scene on disk, not the in-memory editor state. If you've made changes (added objects, modified components) since the last save, those changes are invisible to MCP searches. The search returns stale or incomplete results, leading to incorrect paths or missing objects.

**Pattern:**
- Before any hierarchy search call, save via `save_scene`
- Check for compilation errors first (save during compilation may fail)
- If using `set_property` to wire a new reference, you can skip re-saving — the object path is known from context and the property write triggers its own serialization
