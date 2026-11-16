// Direct3D 12 port of Metal/Effects/GlowComposite.frag.metal (ps_5_1).

struct VertexOut
{
    float4 position : SV_Position;
    float2 uv       : TEXCOORD0;
    float4 color    : TEXCOORD1;
};

struct GlowUniforms
{
    float4 intensity; // x = intensity multiplier, yzw unused
};

Texture2D<float4> tex : register(t0, space2);
SamplerState samp     : register(s0, space2);

cbuffer GlowBuffer : register(b0, space3)
{
    GlowUniforms glow;
};

float4 fragmentMain(VertexOut input) : SV_Target0
{
    float4 texColor = tex.Sample(samp, input.uv) * input.color;
    return texColor * glow.intensity.x;
}
