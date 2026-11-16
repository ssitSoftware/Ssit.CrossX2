// Direct3D 12 port of Metal/Effects/Crt.vert.metal (vs_5_1).

struct VertexIn
{
    float2 position : TEXCOORD0;
    float2 uv       : TEXCOORD1;
    float4 color    : TEXCOORD2;
};

struct VertexOut
{
    float4 position : SV_Position;
    float2 uv       : TEXCOORD0;
    float4 color    : TEXCOORD1;
};

struct ScaleUniforms
{
    float4 scale; // xy = NDC half-extent scale (uniform letterbox fit), zw unused
};

cbuffer ScaleBuffer : register(b0, space1)
{
    ScaleUniforms su;
};

VertexOut vertexMain(VertexIn input)
{
    VertexOut output;
    output.position = float4(input.position * su.scale.xy, 0.0, 1.0);
    output.uv = input.uv;
    output.color = input.color;
    return output;
}
