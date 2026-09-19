#include <metal_stdlib>
using namespace metal;

struct VertexOut
{
    float4 position [[position]];
    float2 uv;
    float4 color;
    float3 screenPosition; // xy = screen-space position, z = height above the observer-facing plane
};

struct LightingUniforms
{
    float4 ambient;                    // rgb ambient color, a - global cell shades
    float4 lightPositionRadius[8];     // xyz = position, w = radius
    float4 lightColorIntensity[8];     // rgb = color, a = intensity
    float4 spotPositionRadius[8];      // xyz = position, w = radius
    float4 spotDirectionAngle[8];      // xy = normalized 2D direction (screen plane), z = cos(outerAngle), w = cos(innerAngle)
    float4 spotColorIntensity[8];      // rgb = color, a = intensity
    float4 lightCount;                 // x = point light count, y = position quantization resolution in pixels, z = spot light count, w - local cell shades
};

fragment float4 fragmentMain(VertexOut in [[stage_in]],
                              texture2d<float> tex [[texture(0)]],
                              sampler samp [[sampler(0)]],
                              constant LightingUniforms &lights [[buffer(0)]])
{
    float4 texColor = tex.sample(samp, in.uv) * in.color;

    // Every vertex is assumed to face the observer directly (left-hand rule, N = -Z).
    const float3 N = float3(0.0, 0.0, -1.0);

    float3 lighting = lights.ambient.rgb;
    int count = int(lights.lightCount.x);
    float resolution = lights.lightCount.y;

	float localCellShading = lights.lightCount.w;
	float globalCellShading = lights.ambient.a;

    float3 fragPosition = resolution > 0.0
        ? floor(in.screenPosition / resolution) * resolution
        : in.screenPosition;

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
		intensity = localCellShading > 0 ? floor(intensity * localCellShading + localCellShading / 2) / localCellShading : intensity;
				
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
        float radius = lights.spotPositionRadius[i].w;
        float2 spotDirection = normalize(lights.spotDirectionAngle[i].xy);
        float cosOuter = lights.spotDirectionAngle[i].z;
        float cosInner = lights.spotDirectionAngle[i].w;
        float3 lightColor = lights.spotColorIntensity[i].rgb;
        float intensity = lights.spotColorIntensity[i].a;

        // The 3D position/distance only feeds the diffuse and distance falloff below;
        // the cone itself is a 2D test in the screen plane.
        float3 toFragment = fragPosition - spotPosition;
        float dist2D = length(toFragment.xy);
        float dist3D = length(toFragment);

        float2 toFragmentDirection2D = dist2D > 0.0001 ? toFragment.xy / dist2D : spotDirection;
        float cosAngle = dot(toFragmentDirection2D, spotDirection);
        float spotFactor = smoothstep(cosOuter, cosInner, cosAngle);

        float3 lightDir = dist3D > 0.0001 ? -toFragment / dist3D : N;
        float diffuse = max(dot(N, lightDir), 0.0);

        float attenuation = clamp(1.0 - dist2D / max(radius, 0.0001), 0.0, 1.0);
        attenuation *= attenuation;
        attenuation *= spotFactor;
        attenuation *= diffuse;

		intensity *= attenuation;
        intensity = localCellShading > 0 ? floor(intensity * localCellShading + localCellShading / 2) / localCellShading : intensity;
                
        lighting += lightColor * intensity;
    }

    lighting = globalCellShading > 0 ? floor(lighting * globalCellShading + globalCellShading / 2) / globalCellShading : lighting;

    return float4(texColor.rgb * lighting, texColor.a);
}
