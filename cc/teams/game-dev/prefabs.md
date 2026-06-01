# Prefab Editing Rules

## Never unpack prefabs without permission

`PrefabUtility.UnpackPrefabInstance` destroys the user's prefab connection. Prefabs exist so changes propagate to all instances — unpacking bypasses that and breaks future updates.

### Why this matters

- **Lost propagation** — Changes to the prefab asset won't reach unpacked instances.
- **Silent drift** — Unpacked instances look identical but are disconnected. The user won't notice until they try to update the prefab later.
- **Reversibility** — There's no "re-pack" button. Once unpacked, the connection is gone.

### What to do instead

| Goal | Approach |
|------|----------|
| Change a serialized field on an instance | `SetProperty` MCP tool (modifies the override on the instance) |
| Add a component to one instance | `add_component` MCP tool (creates an override on the instance) |
| Change that should apply to ALL instances | Modify the prefab asset directly |
| Change needs new child GameObjects | **Ask the user first.** This genuinely requires unpacking or modifying the prefab asset. Present the trade-off. |
| Read-only inspection | `get_game_object_info` or `Read` on the prefab asset — no unpacking needed |

### How to detect prefab instances

Before calling `UnpackPrefabInstance`, check if the object IS a prefab instance:

```csharp
var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
if (root != null)
    // This is a prefab instance — ask before modifying
```

### When it's OK to unpack

Only when the user explicitly says to. Typical scenarios:
- "Unpack this one so I can customize it differently"
- "I don't need the prefab link anymore"
- One-off level design where prefab linkage is irrelevant
