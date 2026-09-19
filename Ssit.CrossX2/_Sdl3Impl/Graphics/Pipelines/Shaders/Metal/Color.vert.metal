#include <metal_stdlib>
using namespace metal;

struct VertexIn
{
    float2 position [[attribute(0)]];
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
    float4 offsetScale; // xy = offset, z = scale, w unused
};

vertex VertexOut vertexMain(VertexIn in [[stage_in]],
                             constant ScreenUniforms &screen [[buffer(0)]])
{
    VertexOut out;

    float2 offset = screen.offsetScale.xy;
    float scale = screen.offsetScale.z;
    float2 transformed = (in.position + offset) * scale;

    float2 ndc = float2(transformed.x / screen.screenSize.x * 2.0 - 1.0,
                         1.0 - transformed.y / screen.screenSize.y * 2.0);
    out.position = float4(ndc, 0.0, 1.0);
    out.color = in.color;
    return out;
}
