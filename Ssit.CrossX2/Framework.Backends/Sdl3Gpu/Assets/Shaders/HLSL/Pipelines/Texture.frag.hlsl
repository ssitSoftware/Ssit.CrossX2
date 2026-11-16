// Direct3D 12 port of Metal/Pipelines/Texture.frag.metal (ps_5_1).
// SDL GPU binding model: fragment textures/samplers live in space2.

struct VertexOut
{
    float4 position : SV_Position;
    float2 uv       : TEXCOORD0;
    float4 color    : TEXCOORD1;
};

Texture2D<float4> tex : register(t0, space2);
SamplerState samp     : register(s0, space2);

float4 fragmentMain(VertexOut input) : SV_Target0
{
    return tex.Sample(samp, input.uv) * input.color;
}
