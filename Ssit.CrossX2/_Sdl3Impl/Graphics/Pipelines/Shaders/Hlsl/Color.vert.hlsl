struct VertexInput
{
    float2 position : TEXCOORD0;
    float4 color    : TEXCOORD1;
};

struct VertexOutput
{
    float4 position : SV_Position;
    float4 color    : TEXCOORD0;
};

cbuffer ScreenUniforms : register(b0, space1)
{
    float4 ScreenSize;  // xy = pixel width/height
    float4 OffsetScale; // xy = offset, z = scale, w unused
};

VertexOutput vertexMain(VertexInput input)
{
    VertexOutput output;

    float2 offset = OffsetScale.xy;
    float scale = OffsetScale.z;
    float2 transformed = (input.position + offset) * scale;

    float2 ndc = float2(transformed.x / ScreenSize.x * 2.0 - 1.0,
                         1.0 - transformed.y / ScreenSize.y * 2.0);

    output.position = float4(ndc, 0.0, 1.0);
    output.color = input.color;
    return output;
}
