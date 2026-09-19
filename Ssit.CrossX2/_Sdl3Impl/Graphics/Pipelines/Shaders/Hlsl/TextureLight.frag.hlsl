struct VertexOutput
{
    float4 position       : SV_Position;
    float2 uv             : TEXCOORD0;
    float4 color          : TEXCOORD1;
    float2 screenPosition : TEXCOORD2;
};

cbuffer LightingUniforms : register(b0, space3)
{
    float4 Ambient;                    // rgb ambient color, a unused
    float4 LightPositionRadius[8];     // xy = screen-space position, z = radius, w unused
    float4 LightColorIntensity[8];     // rgb = color, a = intensity
    float4 SpotPositionRadius[4];      // xy = screen-space position, z = radius, w unused
    float4 SpotDirectionAngle[4];      // xy = normalized direction, z = cos(outerAngle), w = cos(innerAngle)
    float4 SpotColorIntensity[4];      // rgb = color, a = intensity
    float4 LightCount;                 // x = point light count, y = position quantization resolution in pixels, z = spot light count
};

Texture2D<float4> tex : register(t0, space2);
SamplerState samp     : register(s0, space2);

float4 fragmentMain(VertexOutput input) : SV_Target0
{
    float4 texColor = tex.Sample(samp, input.uv) * input.color;

    float3 lighting = Ambient.rgb;
    int count = (int)LightCount.x;
    float resolution = LightCount.y;

    float2 litPosition = resolution > 0.0
        ? floor(input.screenPosition / resolution) * resolution
        : input.screenPosition;

    for (int i = 0; i < 8; i++)
    {
        if (i >= count)
        {
            break;
        }

        float2 lightPosition = LightPositionRadius[i].xy;
        float radius = LightPositionRadius[i].z;
        float3 lightColor = LightColorIntensity[i].rgb;
        float intensity = LightColorIntensity[i].a;

        float dist = distance(litPosition, lightPosition);
        float attenuation = clamp(1.0 - dist / max(radius, 0.0001), 0.0, 1.0);
        attenuation *= attenuation;

        lighting += lightColor * intensity * attenuation;
    }

    int spotCount = (int)LightCount.z;

    for (int i = 0; i < 4; i++)
    {
        if (i >= spotCount)
        {
            break;
        }

        float2 lightPosition = SpotPositionRadius[i].xy;
        float radius = SpotPositionRadius[i].z;
        float2 lightDirection = SpotDirectionAngle[i].xy;
        float cosOuter = SpotDirectionAngle[i].z;
        float cosInner = SpotDirectionAngle[i].w;
        float3 lightColor = SpotColorIntensity[i].rgb;
        float intensity = SpotColorIntensity[i].a;

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
