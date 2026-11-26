// Direct3D 12 port of Metal/Pipelines/Texture.frag.metal (ps_5_1).
// SDL GPU binding model: fragment textures/samplers live in space2, fragment uniform buffers in space3.
//
// Variants (compiled with a #define prepended to the source, see GpuShader):
//   SILHOUETTE        - flat fill with the silhouette color, keeps the texture coverage (occlusion mask).
//   OCCLUSION_CONTOUR - draws the contour of the occlusion mask edges over the covering pixels.

struct VertexOut
{
    float4 position : SV_Position;
    float2 uv       : TEXCOORD0;
    float4 color    : TEXCOORD1;
};

Texture2D<float4> tex : register(t0, space2);
SamplerState samp     : register(s0, space2);

#ifdef SILHOUETTE
cbuffer SilhouetteBuffer : register(b0, space3)
{
    float4 silhouetteColor; // rgb = silhouette color (straight alpha), a = opacity
};
#endif

#ifdef OCCLUSION_CONTOUR
Texture2D<float4> mask : register(t1, space2);
SamplerState maskSamp  : register(s1, space2);

cbuffer OcclusionBuffer : register(b0, space3)
{
    float4 occlusionParameters; // xy = 1 / mask size in pixels, z = contour width in pixels, w = contour opacity
};

// Coverage of a neighbouring mask pixel when it belongs to the same silhouette as the center (same premultiplied color),
// 0 when it's empty or another silhouette - so a contour is drawn also where two silhouettes touch (e.g. feet on the ground).
// The mask is 8-bit: pixels of one silhouette have exactly the same value (the tolerance is for rounding only).
float SameSilhouette(float4 neighbour, float4 center)
{
    float4 difference = abs(neighbour - center);
    return max(max(difference.r, difference.g), max(difference.b, difference.a)) < 1.5 / 255.0 ? neighbour.a : 0.0;
}

// Keep in sync with TextureLight.frag.hlsl and the Metal shaders.
// The mask holds the premultiplied silhouettes of the objects drawn before the covering layer.
// Only pixels inside a silhouette, closer than the contour width to its edge (or to another silhouette), are replaced
// (blended) with its color.
float4 ApplyOcclusionContour(float4 color, float2 position, float4 parameters)
{
    if (color.a <= 0.0)
    {
        return color;
    }

    float2 texel = parameters.xy;
    float2 uv = position * texel;

    float4 center = mask.SampleLevel(maskSamp, uv, 0);

    if (center.a <= 0.0)
    {
        return color;
    }

    float2 offset = parameters.z * texel;

    float neighbours = min(
        min(SameSilhouette(mask.SampleLevel(maskSamp, uv + float2(offset.x, 0.0), 0), center),
            SameSilhouette(mask.SampleLevel(maskSamp, uv - float2(offset.x, 0.0), 0), center)),
        min(SameSilhouette(mask.SampleLevel(maskSamp, uv + float2(0.0, offset.y), 0), center),
            SameSilhouette(mask.SampleLevel(maskSamp, uv - float2(0.0, offset.y), 0), center)));

    // 1 on the silhouette edge (or where it touches another silhouette), 0 inside (scaled by the silhouette opacity).
    float edge = saturate(center.a - neighbours);
    float amount = edge * parameters.w;

    // Premultiplied mask -> straight contour color, premultiplied again with the covering pixel alpha.
    float3 contour = center.rgb / center.a;
    return float4(lerp(color.rgb, contour * color.a, amount), color.a);
}
#endif

float4 fragmentMain(VertexOut input) : SV_Target0
{
    float4 texColor = tex.Sample(samp, input.uv);

#ifdef SILHOUETTE
    // Hard pixel-art edge from the texture alpha (the same threshold as the tile collisions: alpha > 32),
    // faded with the vertex opacity (e.g. a fading object).
    float coverage = step(32.5 / 255.0, texColor.a) * input.color.a * silhouetteColor.a;
    return float4(silhouetteColor.rgb * coverage, coverage);
#else
    float4 color = texColor * input.color;
#ifdef OCCLUSION_CONTOUR
    color = ApplyOcclusionContour(color, input.position.xy, occlusionParameters);
#endif
    return color;
#endif
}
