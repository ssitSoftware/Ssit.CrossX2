// Direct3D 12 port of Metal/Pipelines/TextureLight.frag.metal (ps_5_1).
// Keep the lighting math in sync with ColorLight.frag.hlsl and the Metal shaders.

struct VertexOut
{
    float4 position         : SV_Position;
    float2 uv               : TEXCOORD0;
    float4 color            : TEXCOORD1;
    float3 originalPosition : TEXCOORD2; // xy = original untransformed position, z = height above the observer-facing plane
};

// SDL GPU binding model: fragment sampler N binds to tN/sN in space2.
Texture2D<float4> tex      : register(t0, space2);
SamplerState samp          : register(s0, space2);
Texture2D<float4> lightTex : register(t1, space2);
SamplerState lightSamp     : register(s1, space2);

struct LightingUniforms
{
    float4 ambient;                    // rgb ambient color, a unused
    float4 lightPositionRadius[8];     // xyz = position, w = 1 / radius
    float4 lightColorIntensity[8];     // rgb = color, a = intensity
    float4 spotPositionRadius[8];      // xyz = position, w = radius^2
    float4 spotDirectionAngle[8];      // xy = normalized 2D direction (screen plane), z = cos(outerAngle), w = 1 / (cos(innerAngle) - cos(outerAngle))
    float4 spotColorIntensity[8];      // rgb = color, a = intensity
    float4 spotBulbRadius[8];          // x = bulb radius^2 (world units), y = 1 / radius, zw unused
    float4 lightCount;                 // x = point light count, y = position quantization resolution in pixels, z = spot light count, w unused
    float4 globalColorIntensity[4];    // rgb = color, a = intensity
    float4 globalCount;                // x = global light count, yzw unused
};

// SDL GPU binding model: fragment uniform buffers live in space3.
cbuffer LightingBuffer : register(b0, space3)
{
    LightingUniforms lights;
};

float3 ComputeLighting(float3 originalPosition)
{
    // Every vertex is assumed to face the observer directly (left-hand rule, N = -Z);
    // the diffuse terms below use that normal folded into their formulas.

    float3 lighting = lights.ambient.rgb;
    int count = (int)lights.lightCount.x;
    float resolution = lights.lightCount.y;

    float3 fragPosition = resolution > 0.0
        ? floor(originalPosition / resolution) * resolution
        : originalPosition;

    [loop]
    for (int i = 0; i < 8; i++)
    {
        if (i >= count)
        {
            break;
        }

        float3 lightPosition = lights.lightPositionRadius[i].xyz;
        float invRadius = lights.lightPositionRadius[i].w;      // 1 / radius (precomputed on CPU)

        float3 toLight = lightPosition - fragPosition;
        float dist2DSq = dot(toLight.xy, toLight.xy);

        // Early out: outside the light range (dist2D / radius >= 1).
        if (dist2DSq * invRadius * invRadius >= 1.0)
        {
            continue;
        }

        // dot(N, toLight / |toLight|) with N = (0, 0, -1) reduces to -toLight.z / |toLight|.
        float diffuse = max(-toLight.z * rsqrt(max(dist2DSq + toLight.z * toLight.z, 1e-8)), 0.0);

        // 1 - dist2D / radius, with dist2D = dist2DSq * rsqrt(dist2DSq).
        float attenuation = saturate(1.0 - dist2DSq * rsqrt(max(dist2DSq, 1e-8)) * invRadius);
        attenuation *= attenuation * diffuse;

        lighting += lights.lightColorIntensity[i].rgb * (lights.lightColorIntensity[i].a * attenuation);
    }

    int spotCount = (int)lights.lightCount.z;

    [loop]
    for (int s = 0; s < 8; s++)
    {
        if (s >= spotCount)
        {
            break;
        }

        float3 spotPosition = lights.spotPositionRadius[s].xyz;
        float radiusSq = lights.spotPositionRadius[s].w;       // radius^2 (precomputed on CPU)
        float bulbRadiusSq = lights.spotBulbRadius[s].x;       // bulbRadius^2 (precomputed on CPU)

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
        float2 spotDirection = lights.spotDirectionAngle[s].xy;  // pre-normalized on CPU
        float cosOuter = lights.spotDirectionAngle[s].z;
        float invConeRange = lights.spotDirectionAngle[s].w;     // 1 / (cosInner - cosOuter)

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
        float attenuation = saturate(1.0 - dist2DSq * invDist2D * lights.spotBulbRadius[s].y);
        attenuation *= attenuation * spotFactor * diffuse;

        lighting += lights.spotColorIntensity[s].rgb * (lights.spotColorIntensity[s].a * attenuation);
    }

    int globalLightCount = (int)lights.globalCount.x;

    // Global lights have no position or direction; they contribute uniformly,
    // like ambient light, but each carries its own intensity.
    [loop]
    for (int g = 0; g < 4; g++)
    {
        if (g >= globalLightCount)
        {
            break;
        }

        float3 lightColor = lights.globalColorIntensity[g].rgb;
        float intensity = lights.globalColorIntensity[g].a;

        lighting += lightColor * intensity;
    }

    return lighting;
}

float4 fragmentMain(VertexOut input) : SV_Target0
{
    // Both samples sit outside flow control so implicit-derivative sampling stays valid.
    float4 texColor = tex.Sample(samp, input.uv) * input.color;
    // Premultiplied alpha: the light map fades with the vertex opacity, like the diffuse color does.
    float3 lightMapColor = lightTex.Sample(lightSamp, input.uv).rgb * input.color.a;

    float3 lighting = ComputeLighting(input.originalPosition);
    float3 finalColor = max(texColor.rgb * lighting, lightMapColor);

    return float4(finalColor, texColor.a);
}
