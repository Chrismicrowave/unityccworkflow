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
