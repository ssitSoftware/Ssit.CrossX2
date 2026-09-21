#include <metal_stdlib>
using namespace metal;

struct VertexIn
{
    float3 position [[attribute(0)]];
    float4 color [[attribute(1)]];
    float2 texCoord [[attribute(2)]]; // unused
};

struct VertexOut
{
    float4 position [[position]];
    float4 color;
};

struct ScreenUniforms
{
    float4 screenSize;  // xy = pixel width/height
    float4x4 transform;
};

vertex VertexOut vertexMain(VertexIn in [[stage_in]],
                             constant ScreenUniforms &screen [[buffer(0)]])
{
    VertexOut out;

    float4 localPosition = screen.transform * float4(in.position, 1.0);

    float2 ndc = float2(localPosition.x / screen.screenSize.x * 2.0 - 1.0,
                         1.0 - localPosition.y / screen.screenSize.y * 2.0);
    out.position = float4(ndc, 0.0, 1.0);
    out.color = in.color;
    return out;
}
