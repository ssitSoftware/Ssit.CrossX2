// Direct3D 12 port of Metal/Pipelines/TextureLight.vert.metal (vs_5_1).

struct VertexIn
{
    float3 position : TEXCOORD0;
    float4 color    : TEXCOORD1;
    float2 uv       : TEXCOORD2;
};

struct VertexOut
{
    float4 position         : SV_Position;
    float2 uv               : TEXCOORD0;
    float4 color            : TEXCOORD1;
    float3 originalPosition : TEXCOORD2; // xy = original untransformed position, z = height above the observer-facing plane
};

struct ScreenUniforms
{
    float4 screenSize;  // xy = pixel width/height
    float4 globalColor;
    float4x4 transform;
};

cbuffer ScreenBuffer : register(b0, space1)
{
    ScreenUniforms screen;
};

VertexOut vertexMain(VertexIn input)
{
    VertexOut output;

    float4 localPosition = mul(screen.transform, float4(input.position, 1.0));

    float2 ndc = float2(localPosition.x / screen.screenSize.x * 2.0 - 1.0,
                        1.0 - localPosition.y / screen.screenSize.y * 2.0);
    output.position = float4(ndc, 0.0, 1.0);
    output.uv = input.uv;
    output.color = input.color * screen.globalColor;
    output.originalPosition = input.position;
    return output;
}
