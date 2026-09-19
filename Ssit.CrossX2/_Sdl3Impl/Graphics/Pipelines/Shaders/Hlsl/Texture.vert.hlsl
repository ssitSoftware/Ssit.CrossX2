struct VertexInput
{
    float2 position : TEXCOORD0;
    float2 uv       : TEXCOORD1;
    float4 color    : TEXCOORD2;
};

struct VertexOutput
{
    float4 position : SV_Position;
    float2 uv       : TEXCOORD0;
    float4 color    : TEXCOORD1;
};

cbuffer ScreenUniforms : register(b0, space1)
{
    float4 ScreenSize;  // xy = pixel width/height
    float4 OffsetScale; // xy = offset, z = scale, w unused
    float4 GlobalColor; // color to multiply by all vertices
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
    output.uv = input.uv;
    output.color = input.color * GlobalColor;
    return output;
}
