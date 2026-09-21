#include <metal_stdlib>
using namespace metal;

struct VertexOut
{
    float4 position [[position]];
    float2 uv;
    float4 color;
};

struct GlowUniforms
{
    float4 intensity; // x = intensity multiplier, yzw unused
};

fragment float4 fragmentMain(VertexOut in [[stage_in]],
                              texture2d<float> tex [[texture(0)]],
                              sampler samp [[sampler(0)]],
                              constant GlowUniforms &glow [[buffer(0)]])
{
    float4 texColor = tex.sample(samp, in.uv) * in.color;
    return texColor * glow.intensity.x;
}
