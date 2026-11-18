#include <metal_stdlib>
using namespace metal;

struct VertexOut
{
    float4 position [[position]];
    float4 color;
    float3 originalPosition; // xy = original untransformed position, z = height above the observer-facing plane
};

struct LightingUniforms
{
    float4 ambient;                    // rgb ambient color, a unused
    float4 lightPositionRadius[8];     // xyz = position, w = radius
    float4 lightColorIntensity[8];     // rgb = color, a = intensity
    float4 spotPositionRadius[8];      // xyz = position, w = radius^2
    float4 spotDirectionAngle[8];      // xy = normalized 2D direction (screen plane), z = cos(outerAngle), w = 1 / (cos(innerAngle) - cos(outerAngle))
    float4 spotColorIntensity[8];      // rgb = color, a = intensity
    float4 spotBulbRadius[8];          // x = bulb radius^2 (world units), y = 1 / radius, zw unused
    float4 lightCount;                 // x = point light count, y = position quantization resolution in pixels, z = spot light count, w unused
    float4 globalColorIntensity[4];    // rgb = color, a = intensity
    float4 globalCount;                // x = global light count, yzw unused
};

fragment float4 fragmentMain(VertexOut in [[stage_in]],
                              constant LightingUniforms &lights [[buffer(0)]])
{
    // Every vertex is assumed to face the observer directly (left-hand rule, N = +Z).
    const float3 N = float3(0.0, 0.0, -1.0);

    float3 lighting = lights.ambient.rgb;
    int count = int(lights.lightCount.x);
    float resolution = lights.lightCount.y;

    float3 fragPosition = resolution > 0.0
            ? floor(in.originalPosition / resolution) * resolution
            : in.originalPosition;

    for (int i = 0; i < 8; i++)
    {
        if (i >= count)
        {
            break;
        }

        float3 lightPosition = lights.lightPositionRadius[i].xyz;
        float radius = lights.lightPositionRadius[i].w;
        float3 lightColor = lights.lightColorIntensity[i].rgb;
        float intensity = lights.lightColorIntensity[i].a;

        float3 toLight = lightPosition - fragPosition;
        float dist2D = length(toLight.xy);
        float dist3D = length(toLight);
        float3 lightDir = dist3D > 0.0001 ? toLight / dist3D : N;

        float diffuse = max(dot(N, lightDir), 0.0);

        float attenuation = clamp(1.0 - dist2D / max(radius, 0.0001), 0.0, 1.0);
        attenuation *= attenuation;
        attenuation *= diffuse;

        intensity *= attenuation;
		lighting += lightColor * intensity;
    }

    int spotCount = int(lights.lightCount.z);

    for (int i = 0; i < 8; i++)
    {
        if (i >= spotCount)
        {
            break;
        }

        float3 spotPosition = lights.spotPositionRadius[i].xyz;
        float radiusSq = lights.spotPositionRadius[i].w;       // radius^2 (precomputed on CPU)
        float bulbRadiusSq = lights.spotBulbRadius[i].x;       // bulbRadius^2 (precomputed on CPU)

        float3 toFragment = fragPosition - spotPosition;
        float dist2DSq = dot(toFragment.xy, toFragment.xy);

        // Early out: outside the light range, or inside the physical bulb
        // (the fixture occludes its own light).
        if (dist2DSq >= radiusSq || dist2DSq < bulbRadiusSq)
        {
            continue;
        }

        // The cone itself is a 2D test in the screen plane; one rsqrt replaces normalize + divides.
        float invDist2D = rsqrt(max(dist2DSq, 1e-8));
        float2 spotDirection = lights.spotDirectionAngle[i].xy;  // pre-normalized on CPU
        float cosOuter = lights.spotDirectionAngle[i].z;
        float invConeRange = lights.spotDirectionAngle[i].w;     // 1 / (cosInner - cosOuter)

        float spotFactor = saturate((dot(toFragment.xy, spotDirection) * invDist2D - cosOuter) * invConeRange);

        // Early out: outside the cone.
        if (spotFactor <= 0.0)
        {
            continue;
        }

        spotFactor = spotFactor * spotFactor * (3.0 - 2.0 * spotFactor); // smoothstep(cosOuter, cosInner, cosAngle)

        // dot(N, -toFragment / |toFragment|) with N = (0, 0, -1) reduces to toFragment.z / |toFragment|.
        float diffuse = max(toFragment.z * rsqrt(max(dist2DSq + toFragment.z * toFragment.z, 1e-8)), 0.0);

        // 1 - dist2D / radius, with dist2D = dist2DSq * invDist2D and spotBulbRadius.y = 1 / radius.
        float attenuation = saturate(1.0 - dist2DSq * invDist2D * lights.spotBulbRadius[i].y);
        attenuation *= attenuation * spotFactor * diffuse;

        lighting += lights.spotColorIntensity[i].rgb * (lights.spotColorIntensity[i].a * attenuation);
    }

    int globalLightCount = int(lights.globalCount.x);

    // Global lights have no position or direction; they contribute uniformly,
    // like ambient light, but each carries its own intensity.
    for (int i = 0; i < 4; i++)
    {
        if (i >= globalLightCount)
        {
            break;
        }

        float3 lightColor = lights.globalColorIntensity[i].rgb;
        float intensity = lights.globalColorIntensity[i].a;

        lighting += lightColor * intensity;
    }

    return float4(in.color.rgb * lighting, in.color.a);
}
