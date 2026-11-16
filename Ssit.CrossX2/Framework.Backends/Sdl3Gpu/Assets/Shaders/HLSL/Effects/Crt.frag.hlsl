// Direct3D 12 port of Metal/Effects/Crt.frag.metal (ps_5_1).
// Uses SampleLevel(…, 0) because the samples follow an early-out branch; the CRT source
// is a single-level render target, so this matches the Metal output.

struct VertexOut
{
    float4 position : SV_Position;
    float2 uv       : TEXCOORD0;
    float4 color    : TEXCOORD1;
};

struct CrtUniforms
{
    float4 distortion; // x = barrel distortion, y = RGB displacement, z = scanline intensity, w = vignette strength
    float4 params;     // x = output pixels per source texel (uniform fit scale), y = gamma, z = source scale, w = bleed factor
    float4 resolution; // x = fitted output height in pixels, y = lightness multiplier, z = saturation (0..1), w unused
};

Texture2D<float4> tex : register(t0, space2);
SamplerState samp     : register(s0, space2);

cbuffer CrtBuffer : register(b0, space3)
{
    CrtUniforms crt;
};

float4 fragmentMain(VertexOut input) : SV_Target0
{
    float barrel = crt.distortion.x;
    float rgbShift = crt.distortion.y;
    float scanline = crt.distortion.z;
    float vignette = crt.distortion.w;

    float2 centered = input.uv * 2.0 - 1.0;
    float r2 = dot(centered, centered);

    float2 warped = centered * (1.0 + barrel * r2);
    float2 uv = warped * 0.5 + 0.5;

    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
    {
        return float4(0.0, 0.0, 0.0, 1.0) * input.color.a;
    }

    float centeredLength = length(centered);
    float2 dir2 = centeredLength > 0.0001 ? centered / centeredLength : float2(0.0, 0.0);

    float2 offset = dir2 * rgbShift * 0.005;

    float rCh = tex.SampleLevel(samp, uv + offset, 0).r;
    float gCh = tex.SampleLevel(samp, uv, 0).g;
    float bCh = tex.SampleLevel(samp, uv - offset, 0).b;
    float aCh = tex.SampleLevel(samp, uv, 0).a;

    float3 rgb = float3(rCh, gCh, bCh);

    // Each scanline band (bright or dark half of the cycle) spans half a source texel,
    // so a full bright+dark cycle spans one source texel's worth of output pixels.
    // The phase is driven by the barrel-warped y (scaled back into pixel space) so the
    // scanlines bow along with the tube curvature.
    float pixelScale = max(crt.params.x * crt.params.z * 1.5, 0.0001);
    float warpedPixelY = warped.y * (crt.resolution.x * 0.5);
    float scanlinePhase = 0.5 + 0.5 * sin(warpedPixelY * (2.0 * 3.14159265 / pixelScale));

    float vignetteEffect = saturate(1.0 - vignette * r2);
    rgb *= vignetteEffect;

    float gamma = crt.params.y;
    float lightnessMultiplier = crt.resolution.y;
    rgb = pow(max(rgb, 0.0), gamma) * lightnessMultiplier;

    float saturation = crt.resolution.z;
    float desaturatedLuma = dot(rgb, float3(0.299, 0.587, 0.114));
    rgb = lerp(desaturatedLuma.xxx, rgb, saturation);

    // Bright pixels bleed through the dark scanline gap instead of being darkened as much.
    float luma = dot(rgb, float3(0.299, 0.587, 0.114));
    float bleed = saturate(luma);
    bleed *= crt.params.w;

    float scanlineDarken = scanline * (1.0 - scanlinePhase) * (1.0 - bleed);

    rgb *= (1.0 - scanlineDarken);

    return float4(rgb, aCh) * input.color;
}
