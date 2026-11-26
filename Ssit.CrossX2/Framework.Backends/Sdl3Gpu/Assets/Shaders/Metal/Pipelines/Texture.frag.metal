#include <metal_stdlib>
using namespace metal;

// Variants (compiled with a #define prepended to the source, see GpuShader):
//   SILHOUETTE        - flat fill with the silhouette color, keeps the texture coverage (occlusion mask).
//   OCCLUSION_CONTOUR - draws the contour of the occlusion mask edges over the covering pixels.

struct VertexOut
{
    float4 position [[position]];
    float2 uv;
    float4 color;
};

#ifdef SILHOUETTE
struct SilhouetteUniforms
{
    float4 color; // rgb = silhouette color (straight alpha), a = opacity
};
#endif

#ifdef OCCLUSION_CONTOUR
struct OcclusionUniforms
{
    float4 parameters; // xy = 1 / mask size in pixels, z = contour width in pixels, w = contour opacity
};

// Coverage of a neighbouring mask pixel when it belongs to the same silhouette as the center (same premultiplied color),
// 0 when it's empty or another silhouette - so a contour is drawn also where two silhouettes touch (e.g. feet on the ground).
// The mask is 8-bit: pixels of one silhouette have exactly the same value (the tolerance is for rounding only).
float SameSilhouette(float4 neighbour, float4 center)
{
    float4 difference = abs(neighbour - center);
    return max(max(difference.r, difference.g), max(difference.b, difference.a)) < 1.5 / 255.0 ? neighbour.a : 0.0;
}

// Keep in sync with TextureLight.frag.metal and the HLSL shaders.
// The mask holds the premultiplied silhouettes of the objects drawn before the covering layer.
// Only pixels inside a silhouette, closer than the contour width to its edge (or to another silhouette), are replaced
// (blended) with its color.
float4 ApplyOcclusionContour(float4 color, float2 position, texture2d<float> mask, sampler maskSamp, float4 parameters)
{
    if (color.a <= 0.0)
    {
        return color;
    }

    float2 texel = parameters.xy;
    float2 uv = position * texel;

    float4 center = mask.sample(maskSamp, uv, level(0));

    if (center.a <= 0.0)
    {
        return color;
    }

    float2 offset = parameters.z * texel;

    float neighbours = min(
        min(SameSilhouette(mask.sample(maskSamp, uv + float2(offset.x, 0.0), level(0)), center),
            SameSilhouette(mask.sample(maskSamp, uv - float2(offset.x, 0.0), level(0)), center)),
        min(SameSilhouette(mask.sample(maskSamp, uv + float2(0.0, offset.y), level(0)), center),
            SameSilhouette(mask.sample(maskSamp, uv - float2(0.0, offset.y), level(0)), center)));

    // 1 on the silhouette edge (or where it touches another silhouette), 0 inside (scaled by the silhouette opacity).
    float edge = saturate(center.a - neighbours);
    float amount = edge * parameters.w;

    // Premultiplied mask -> straight contour color, premultiplied again with the covering pixel alpha.
    float3 contour = center.rgb / center.a;
    return float4(mix(color.rgb, contour * color.a, amount), color.a);
}
#endif

fragment float4 fragmentMain(VertexOut in [[stage_in]],
                              texture2d<float> tex [[texture(0)]],
                              sampler samp [[sampler(0)]]
#ifdef SILHOUETTE
                              , constant SilhouetteUniforms &silhouette [[buffer(0)]]
#endif
#ifdef OCCLUSION_CONTOUR
                              , texture2d<float> mask [[texture(1)]]
                              , sampler maskSamp [[sampler(1)]]
                              , constant OcclusionUniforms &occlusion [[buffer(0)]]
#endif
                              )
{
    float4 texColor = tex.sample(samp, in.uv);

#ifdef SILHOUETTE
    // Hard pixel-art edge from the texture alpha (the same threshold as the tile collisions: alpha > 32),
    // faded with the vertex opacity (e.g. a fading object).
    float coverage = step(32.5 / 255.0, texColor.a) * in.color.a * silhouette.color.a;
    return float4(silhouette.color.rgb * coverage, coverage);
#else
    float4 color = texColor * in.color;
#ifdef OCCLUSION_CONTOUR
    color = ApplyOcclusionContour(color, in.position.xy, mask, maskSamp, occlusion.parameters);
#endif
    return color;
#endif
}
