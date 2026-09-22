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
    float4 params; // x = output pixels per source texel (uniform fit scale), y = gamma, zw unused
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
    float scale = crt.params.z;

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
    //dir = (dir + float2(1,0)) /  2;
    float2 offset = dir * rgbShift * 0.01;

    float rCh = tex.sample(samp, uv + offset).r;
    float gCh = tex.sample(samp, uv).g;
    float bCh = tex.sample(samp, uv - offset).b;
    float aCh = tex.sample(samp, uv).a;

    float3 rgb = float3(rCh, gCh, bCh);

    // Each scanline band (bright or dark half of the cycle) spans half a source texel,
    // so a full bright+dark cycle spans one source texel's worth of output pixels.
    float pixelScale = max(crt.params.x * crt.params.z * 1.5, 0.0001);
    float scanlinePhase = 0.5 + 0.5 * sin(in.position.y * (2.0 * 3.14159265 / pixelScale));

    float vignetteEffect = saturate(1.0 - vignette * r2);
    rgb *= vignetteEffect;

	float restoreLightness = crt.params.y;
    float gamma = sqrt(sqrt(max(restoreLightness, 0.1)));
    rgb = pow(max(rgb, 0.0), gamma) * restoreLightness * restoreLightness;

	// Bright pixels bleed through the dark scanline gap instead of being darkened as much.
	float luma = dot(rgb, float3(0.299, 0.587, 0.114));
	float bleed = saturate(luma);
	bleed *= crt.params.w;
	
	float scanlineDarken = scanline * (1.0 - scanlinePhase) * (1.0 - bleed);
	
	rgb *= (1.0 - scanlineDarken);

    return float4(rgb, aCh) * in.color;
}
