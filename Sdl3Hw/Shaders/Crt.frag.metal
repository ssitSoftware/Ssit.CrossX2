#include <metal_stdlib>
using namespace metal;

struct VertexOut
{
    float4 position [[position]];
    float2 uv;
    float4 color;
};

struct CrtUniforms
{
    float4 distortion; // x = barrel distortion, y = RGB displacement, z = scanline intensity, w = vignette strength
    float4 params; // x = output pixels per source texel (uniform fit scale), yzw unused
};

fragment float4 fragmentMain(VertexOut in [[stage_in]],
                              texture2d<float> tex [[texture(0)]],
                              sampler samp [[sampler(0)]],
                              constant CrtUniforms &crt [[buffer(0)]])
{
    float barrel = crt.distortion.x;
    float rgbShift = crt.distortion.y;
    float scanline = crt.distortion.z;
    float vignette = crt.distortion.w;

    float2 centered = in.uv * 2.0 - 1.0;
    float r2 = dot(centered, centered);

    float2 warped = centered * (1.0 + barrel * r2);
    float2 uv = warped * 0.5 + 0.5;

    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
    {
        return float4(0.0, 0.0, 0.0, 1.0) * in.color.a;
    }

    float centeredLength = length(centered);
    float2 dir = centeredLength > 0.0001 ? centered / centeredLength : float2(0.0, 0.0);
    float2 offset = dir * rgbShift * 0.01;

    float rCh = tex.sample(samp, uv + offset).r;
    float gCh = tex.sample(samp, uv).g;
    float bCh = tex.sample(samp, uv - offset).b;
    float aCh = tex.sample(samp, uv).a;

    float3 rgb = float3(rCh, gCh, bCh);

    // Each scanline band (bright or dark half of the cycle) spans half a source texel,
    // so a full bright+dark cycle spans one source texel's worth of output pixels.
    float pixelScale = max(crt.params.x, 0.0001);
    float scanlineEffect = 1.0 - scanline * (0.5 + 0.5 * sin(in.position.y * (2.0 * 3.14159265 / pixelScale)));
    rgb *= scanlineEffect;

    float vignetteEffect = saturate(1.0 - vignette * r2);
    rgb *= vignetteEffect;

    return float4(rgb, aCh) * in.color;
}
