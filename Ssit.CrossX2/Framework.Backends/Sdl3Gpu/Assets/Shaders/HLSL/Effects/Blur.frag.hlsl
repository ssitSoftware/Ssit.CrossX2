// Direct3D 12 port of Metal/Effects/Blur.frag.metal (ps_5_1).

struct VertexOut
{
    float4 position : SV_Position;
    float2 uv       : TEXCOORD0;
    float4 color    : TEXCOORD1;
};

struct BlurUniforms
{
    float4 direction; // xy = per-tap texel step, zw unused
};

Texture2D<float4> tex : register(t0, space2);
SamplerState samp     : register(s0, space2);

cbuffer BlurBuffer : register(b0, space3)
{
    BlurUniforms blur;
};

static const float weights[5] = { 0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162 };

float4 fragmentMain(VertexOut input) : SV_Target0
{
    float2 stepSize = blur.direction.xy;

    float4 result = tex.Sample(samp, input.uv) * weights[0];

    [unroll]
    for (int i = 1; i < 5; i++)
    {
        float2 offset = stepSize * (float)i;
        result += tex.Sample(samp, input.uv + offset) * weights[i];
        result += tex.Sample(samp, input.uv - offset) * weights[i];
    }

    return result * input.color;
}
