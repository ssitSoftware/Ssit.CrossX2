#include <metal_stdlib>
using namespace metal;

struct VertexOutput
{
    float4 position [[position]];
    float3 color;
};

fragment float4 fragmentMain(VertexOutput in [[stage_in]])
{
    return float4(in.color, 1.0);
}
