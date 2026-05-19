# Game Dev — Tools Patterns

General Unity editor tool patterns reusable across projects.

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
