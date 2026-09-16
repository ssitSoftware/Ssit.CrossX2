#include <metal_stdlib>
using namespace metal;

struct VertexIn
{
    float2 position [[attribute(0)]];
    float2 uv [[attribute(1)]];
    float4 color [[attribute(2)]];
};

struct VertexOut
{
    float4 position [[position]];
    float2 uv;
    float4 color;
};

struct ScaleUniforms
{
    float4 scale; // xy = NDC half-extent scale (uniform letterbox fit), zw unused
};

vertex VertexOut vertexMain(VertexIn in [[stage_in]],
                             constant ScaleUniforms &su [[buffer(0)]])
{
    VertexOut out;
    out.position = float4(in.position * su.scale.xy, 0.0, 1.0);
    out.uv = in.uv;
    out.color = in.color;
    return out;
}
