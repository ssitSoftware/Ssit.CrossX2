#include <metal_stdlib>
using namespace metal;

struct VertexInput
{
    float3 position [[attribute(0)]];
    float3 color [[attribute(1)]];
};

struct VertexOutput
{
    float4 position [[position]];
    float3 color;
};

vertex VertexOutput vertexMain(VertexInput in [[stage_in]])
{
    VertexOutput out;
    out.position = float4(in.position, 1.0);
    out.color = in.color;
    return out;
}
