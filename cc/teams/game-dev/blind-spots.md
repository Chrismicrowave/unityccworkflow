# AI Blind Spots in Unity

This AI has systematic blind spots in Unity game development. These areas require user correction during sessions — the AI will confidently write code that compiles but produces incorrect visual/spatial results.

## Canvas↔3D coordinate conversion

**The pattern:** Passes `rectTransform.GetWorldCorners()` through `camera.WorldToViewportPoint()` without considering that ScreenSpace canvas corners are pixel positions at z=0, not 3D world positions.

**The fix:** Use `RectTransformUtility.WorldToScreenPoint()` to convert UI corners to screen pixels first, then divide by `Screen.width/height` for viewport coordinates.

See `systems.md` → "Canvas↔world coordinate conversion" for the full code pattern.

## Visual output

Cannot verify shaders, lighting, post-processing, or material appearance. Compilation success ≠ visual correctness. Screenshots are essential for catching issues.

## Hierarchy/active state during Play Mode

May forget that:
- Objects enabled in the Editor hierarchy are script-disabled at runtime
- Active/inactive hierarchy state matters for `GetWorldCorners`, canvas queries, and `Find` operations
- `GameObject.activeInHierarchy` vs `activeSelf` distinction

## Animation blending & transition timing

Can create AnimatorController state machines and blend trees correctly, but transition durations, exit times, curve shapes, and crossfade parameters will need manual tuning.

## Performance numbers

Knows *what* causes performance cost (draw calls, GC allocations, batching breaks, overdraw) but cannot predict *how much* without a profiler. Estimates may be off by orders of magnitude.

## Build/platform issues

Little practical experience with:
- IL2CPP compilation quirks and AOT issues
- Managed code stripping edge cases
- Platform-specific defines and API differences
- Asset bundle versioning and dependencies
