#include <metal_stdlib>
using namespace metal;

struct VertexOut
{
    float4 position [[position]];
    float2 uv;
    float4 color;
    float2 screenPosition;
};

struct LightingUniforms
{
    float4 ambient;                    // rgb ambient color, a unused
    float4 lightPositionRadius[8];     // xy = screen-space position, z = radius, w unused
    float4 lightColorIntensity[8];     // rgb = color, a = intensity
    float4 spotPositionRadius[4];      // xy = screen-space position, z = radius, w unused
    float4 spotDirectionAngle[4];      // xy = normalized direction, z = cos(outerAngle), w = cos(innerAngle)
    float4 spotColorIntensity[4];      // rgb = color, a = intensity
    float4 lightCount;                 // x = point light count, y = position quantization resolution in pixels, z = spot light count
};

fragment float4 fragmentMain(VertexOut in [[stage_in]],
                              texture2d<float> tex [[texture(0)]],
                              sampler samp [[sampler(0)]],
                              constant LightingUniforms &lights [[buffer(0)]])
{
    float4 texColor = tex.sample(samp, in.uv) * in.color;

    float3 lighting = lights.ambient.rgb;
    int count = int(lights.lightCount.x);
    float resolution = lights.lightCount.y;

    float2 litPosition = resolution > 0.0
        ? floor(in.screenPosition / resolution) * resolution
        : in.screenPosition;

    for (int i = 0; i < 8; i++)
    {
        if (i >= count)
        {
            break;
        }

        float2 lightPosition = lights.lightPositionRadius[i].xy;
        float radius = lights.lightPositionRadius[i].z;
        float3 lightColor = lights.lightColorIntensity[i].rgb;
        float intensity = lights.lightColorIntensity[i].a;

        float dist = distance(litPosition, lightPosition);
        float attenuation = clamp(1.0 - dist / max(radius, 0.0001), 0.0, 1.0);
        attenuation *= attenuation;

        lighting += lightColor * intensity * attenuation;
    }

    int spotCount = int(lights.lightCount.z);

    for (int i = 0; i < 4; i++)
    {
        if (i >= spotCount)
        {
            break;
        }

        float2 lightPosition = lights.spotPositionRadius[i].xy;
        float radius = lights.spotPositionRadius[i].z;
        float2 lightDirection = lights.spotDirectionAngle[i].xy;
        float cosOuter = lights.spotDirectionAngle[i].z;
        float cosInner = lights.spotDirectionAngle[i].w;
        float3 lightColor = lights.spotColorIntensity[i].rgb;
        float intensity = lights.spotColorIntensity[i].a;

        float2 toFragment = litPosition - lightPosition;
        float dist = length(toFragment);
        float2 toFragmentDirection = dist > 0.0001 ? toFragment / dist : lightDirection;

        float cosAngle = dot(toFragmentDirection, lightDirection);
        float spotFactor = smoothstep(cosOuter, cosInner, cosAngle);

        float attenuation = clamp(1.0 - dist / max(radius, 0.0001), 0.0, 1.0);
        attenuation *= attenuation;
        attenuation *= spotFactor;

        lighting += lightColor * intensity * attenuation;
    }

    return float4(texColor.rgb * lighting, texColor.a);
}
