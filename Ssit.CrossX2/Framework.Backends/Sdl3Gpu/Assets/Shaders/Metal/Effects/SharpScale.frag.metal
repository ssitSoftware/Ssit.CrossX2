#include <metal_stdlib>
using namespace metal;

// "Sharp bilinear" upscaling: every source texel stays a solid block and only the (at most one output pixel
// wide) seam between neighbouring texels is blended, so non-integer scales look crisp without uneven pixels.
// Keep in sync with HLSL/Effects/SharpScale.frag.hlsl.

struct VertexOut
{
    float4 position [[position]];
    float2 uv;
    float4 color;
};

struct SharpScaleUniforms
{
    float4 textureSize; // xy = source size in texels, zw = 1 / size
    float4 scale;       // xy = output pixels per source texel, zw unused
    float4 grading;     // x = gamma, y = lightness multiplier, z = saturation (0..1), w = 1 to apply grading
};

fragment float4 fragmentMain(VertexOut in [[stage_in]],
                              texture2d<float> tex [[texture(0)]],
                              sampler samp [[sampler(0)]],
                              constant SharpScaleUniforms &sharp [[buffer(0)]])
{
    float2 texel = in.uv * sharp.textureSize.xy;
    float2 scale = max(sharp.scale.xy, 1.0);

    // Inside this distance from the texel centre the sample snaps to the centre; beyond it, it ramps to
    // the neighbour across exactly one output pixel.
    float2 region = 0.5 - 0.5 / scale;
    float2 centerDistance = fract(texel) - 0.5;
    float2 offset = (centerDistance - clamp(centerDistance, -region, region)) * scale + 0.5;

    float2 uv = (floor(texel) + offset) * sharp.textureSize.zw;

    // level(0): the snapped uv has discontinuous derivatives, and the source has a single mip level anyway.
    float4 color = tex.sample(samp, uv, level(0));

    // Colour grading, same maths and order as Crt.frag (gamma, lightness, then saturation).
    if (sharp.grading.w > 0.5)
    {
        float3 rgb = pow(max(color.rgb, 0.0), sharp.grading.x) * sharp.grading.y;
        float luma = dot(rgb, float3(0.299, 0.587, 0.114));
        color.rgb = mix(float3(luma), rgb, sharp.grading.z);
    }

    return color * in.color;
}
