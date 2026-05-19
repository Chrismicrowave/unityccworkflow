# Game Dev — Shader Patterns

General Unity URP shader patterns reusable across projects.

## UV-Space Texel Size (`_ScreenParams`)

**Never use `_ScreenParams.zw` as texel size.** The `zw` components are NOT `(1/width, 1/height)` — they equal `(1 + 1/width, 1 + 1/height)` ≈ `(1, 1)`. Using them in a Sobel kernel samples the same pixel for all taps, producing no edge detection, or ~1 UV unit away producing false edges everywhere.

```hlsl
// WRONG — _ScreenParams.zw ≈ (1, 1)
float2 texelSize = _ScreenParams.zw * _LineWidth;

// CORRECT — explicit reciprocal
float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y) * _LineWidth;
```

**Where it applies**: Any UV-space pixel offset — Sobel kernels, bilateral blur, bloom, depth-of-field, FXAA, motion blur offsets.

## Depth Sobel Edge Detection

4-tap cross kernel computing depth gradient magnitude. Output is a scalar edge value (0 = flat, 1+ = edge).

```hlsl
// Requires: DeclareDepthTexture.hlsl for SampleSceneDepth()
float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y) * _LineWidth;

float dL = SampleSceneDepth(uv - float2(texelSize.x, 0));
float dR = SampleSceneDepth(uv + float2(texelSize.x, 0));
float dD = SampleSceneDepth(uv - float2(0, texelSize.y));
float dU = SampleSceneDepth(uv + float2(0, texelSize.y));

float edge = length(float2(dR - dL, dU - dD));
edge = saturate(edge / _Threshold);
```

**Properties**: Only detects edges at depth discontinuities — flat surfaces produce zero output. Combine with normal-based Sobel for crease detection. `_Threshold` controls sensitivity (lower = fewer, sharper edges).

## Single-Pass Screen-Space Wiggle

Displace sampling coordinates with FPS-quantized noise before the main computation. Works for any screen-space effect where the source signal is sparse (edges, highlights, etc.) — the displacement only affects regions that already have signal, avoiding "fullscreen noise" artifacts.

```hlsl
float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

// FPS-quantized time for stepped animation
float wiggleTime = _WiggleFPS > 0.001
    ? floor(_Time.y * _WiggleFPS + 0.001) / _WiggleFPS
    : _Time.y;

// Noise sample at animated UV
float2 noiseUV = uv * _WiggleScale + wiggleTime * _WiggleSpeed;
float wn = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

// Displace center position by N pixels (pixel units × texelSize = UV offset)
float2 wiggleOffset = (wn - 0.5) * _WiggleAmount * texelSize;
float2 wUV = uv + wiggleOffset;

// ... then use wUV for Sobel taps / sampling
```

**Key**: `_WiggleAmount` in **pixel units** (Range 0-8), not fraction-of-texel. The `* texelSize` converts to UV space. This avoids oversampling artifacts.

## FPS Quantization Pattern

Produces stepped, hand-drawn animation for shader parameters (wiggle, boil, animated masks). Frames snap at integer intervals of the target FPS rather than smoothly interpolating.

```hlsl
float fps = 12.0; // target frames per second
float quantizedTime = floor(_Time.y * fps + 0.001) / fps;
```

The `+ 0.001` epsilon prevents floating-point rounding issues at frame boundaries. The result can be used to drive noise UV scrolling, parameter animation, or any time-varying shader effect.

## FullScreenPassRendererFeature Setup

When using URP 17's `FullScreenPassRendererFeature` for custom shader-based effects:

```
FullScreenPassRendererFeature:
  - fetchColorBuffer: true     // populates _BlitTexture with scene color
  - requirements: Normal       // requests depth + normal textures
  - injectionPoint: BeforeRenderingPostProcessing
  - passMaterial: Material with Hidden/ shader
```

- `_BlitTexture` is the scene color input (set by the feature, not by shader property)
- Depth texture via `DeclareDepthTexture.hlsl` → `SampleSceneDepth()`
- Use `ZTest Always ZWrite Off Cull Off` for fullscreen passes
- Editor setup scripts (`SetupXxx.cs`) can add this feature programmatically via `ScriptableRendererFeature.CreateInstance<FullScreenPassRendererFeature>()`

## Noise Texture Requirements

For screen-space noise effects (wiggle, breakup, boil):

| Property | Setting |
|---|---|
| Format | R8 (single channel, 8-bit) |
| sRGB | Off (linear) |
| Filter | Point (no interpolation — stepped noise matches hand-drawn look) |
| Wrap | Repeat |
| Midpoint | 0.5 (for symmetric remap to [-0.5, 0.5]) |
| Size | 64×64 to 128×128 (small is fine; tiled via UV scale) |

Generate via editor script: `Texture2D` with `TextureFormat.R8`, set pixels to random values, encode as PNG or serialize as `.asset`.
