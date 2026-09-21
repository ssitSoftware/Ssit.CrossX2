#include <metal_stdlib>
using namespace metal;

struct VertexOut
{
    float4 position [[position]];
    float2 uv;
    float4 color;
};

struct BlurUniforms
{
    float4 direction; // xy = per-tap texel step, zw unused
};

fragment float4 fragmentMain(VertexOut in [[stage_in]],
                              texture2d<float> tex [[texture(0)]],
                              sampler samp [[sampler(0)]],
                              constant BlurUniforms &blur [[buffer(0)]])
{
    float2 step = blur.direction.xy;

    constexpr float weights[5] = { 0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162 };

    float4 result = tex.sample(samp, in.uv) * weights[0];

    for (int i = 1; i < 5; i++)
    {
        float2 offset = step * float(i);
        result += tex.sample(samp, in.uv + offset) * weights[i];
        result += tex.sample(samp, in.uv - offset) * weights[i];
    }

    return result * in.color;
}
