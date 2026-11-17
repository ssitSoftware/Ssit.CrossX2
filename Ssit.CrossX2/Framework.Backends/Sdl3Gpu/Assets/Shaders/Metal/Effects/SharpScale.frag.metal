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
    return tex.sample(samp, uv, level(0)) * in.color;
}
