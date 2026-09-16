using System.Numerics;
using System.Runtime.InteropServices;
using CrossX2.Graphics.Pipelines;
using SDL;
using static SDL.SDL3;

namespace Sdl3Hw;

public sealed unsafe class LightingGpuPipeline : TextureGpuPipeline
{
    public Lighting2DParameters Parameters
    {
        get => field ??= new Lighting2DParameters();
        set;
    }

    public LightingGpuPipeline(SDL_GPUDevice* device, SDL_Window* window)
        : base(device, window, "Shaders.Screen.vert.metal", "Shaders.Light.frag.metal", fragmentUniformBuffers: 1)
    {
    }

    public override void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures)
    {
        base.Bind(commandBuffer, renderPass, textures);

        IReadOnlyList<PointLight2D> lights = Parameters.Lights;
        int count = Math.Min(lights.Count, Lighting2DParameters.MaxLights);

        Span<Vector4> positionRadius = stackalloc Vector4[Lighting2DParameters.MaxLights];
        Span<Vector4> colorIntensity = stackalloc Vector4[Lighting2DParameters.MaxLights];

        for (int i = 0; i < count; i++)
        {
            PointLight2D light = lights[i];
            positionRadius[i] = new Vector4(light.Position.X, light.Position.Y, light.Radius, 0f);
            colorIntensity[i] = new Vector4(light.Color.Rf, light.Color.Gf, light.Color.Bf, light.Intensity);
        }

        IReadOnlyList<SpotLight2D> spotLights = Parameters.SpotLights;
        int spotCount = Math.Min(spotLights.Count, Lighting2DParameters.MaxSpotLights);

        Span<Vector4> spotPositionRadius = stackalloc Vector4[Lighting2DParameters.MaxSpotLights];
        Span<Vector4> spotDirectionAngle = stackalloc Vector4[Lighting2DParameters.MaxSpotLights];
        Span<Vector4> spotColorIntensity = stackalloc Vector4[Lighting2DParameters.MaxSpotLights];

        for (int i = 0; i < spotCount; i++)
        {
            SpotLight2D spot = spotLights[i];
            spotPositionRadius[i] = new Vector4(spot.Position.X, spot.Position.Y, spot.Radius, 0f);
            spotDirectionAngle[i] = new Vector4(spot.Direction.X, spot.Direction.Y, MathF.Cos(spot.OuterAngle), MathF.Cos(spot.InnerAngle));
            spotColorIntensity[i] = new Vector4(spot.Color.Rf, spot.Color.Gf, spot.Color.Bf, spot.Intensity);
        }

        var uniforms = new LightingUniforms
        {
            Ambient = new Vector4(Parameters.Ambient.Rf, Parameters.Ambient.Gf, Parameters.Ambient.Bf, 0f),
            LightPositionRadius0 = positionRadius[0],
            LightPositionRadius1 = positionRadius[1],
            LightPositionRadius2 = positionRadius[2],
            LightPositionRadius3 = positionRadius[3],
            LightPositionRadius4 = positionRadius[4],
            LightPositionRadius5 = positionRadius[5],
            LightPositionRadius6 = positionRadius[6],
            LightPositionRadius7 = positionRadius[7],
            LightColorIntensity0 = colorIntensity[0],
            LightColorIntensity1 = colorIntensity[1],
            LightColorIntensity2 = colorIntensity[2],
            LightColorIntensity3 = colorIntensity[3],
            LightColorIntensity4 = colorIntensity[4],
            LightColorIntensity5 = colorIntensity[5],
            LightColorIntensity6 = colorIntensity[6],
            LightColorIntensity7 = colorIntensity[7],
            SpotPositionRadius0 = spotPositionRadius[0],
            SpotPositionRadius1 = spotPositionRadius[1],
            SpotPositionRadius2 = spotPositionRadius[2],
            SpotPositionRadius3 = spotPositionRadius[3],
            SpotDirectionAngle0 = spotDirectionAngle[0],
            SpotDirectionAngle1 = spotDirectionAngle[1],
            SpotDirectionAngle2 = spotDirectionAngle[2],
            SpotDirectionAngle3 = spotDirectionAngle[3],
            SpotColorIntensity0 = spotColorIntensity[0],
            SpotColorIntensity1 = spotColorIntensity[1],
            SpotColorIntensity2 = spotColorIntensity[2],
            SpotColorIntensity3 = spotColorIntensity[3],
            LightCount = new Vector4(count, Parameters.Resolution, spotCount, 0f),
        };

        SDL_PushGPUFragmentUniformData(commandBuffer, 0, (IntPtr)(&uniforms), (uint)sizeof(LightingUniforms));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LightingUniforms
    {
        public Vector4 Ambient;
        public Vector4 LightPositionRadius0;
        public Vector4 LightPositionRadius1;
        public Vector4 LightPositionRadius2;
        public Vector4 LightPositionRadius3;
        public Vector4 LightPositionRadius4;
        public Vector4 LightPositionRadius5;
        public Vector4 LightPositionRadius6;
        public Vector4 LightPositionRadius7;
        public Vector4 LightColorIntensity0;
        public Vector4 LightColorIntensity1;
        public Vector4 LightColorIntensity2;
        public Vector4 LightColorIntensity3;
        public Vector4 LightColorIntensity4;
        public Vector4 LightColorIntensity5;
        public Vector4 LightColorIntensity6;
        public Vector4 LightColorIntensity7;
        public Vector4 SpotPositionRadius0;
        public Vector4 SpotPositionRadius1;
        public Vector4 SpotPositionRadius2;
        public Vector4 SpotPositionRadius3;
        public Vector4 SpotDirectionAngle0; // xy = normalized direction, z = cos(outerAngle), w = cos(innerAngle)
        public Vector4 SpotDirectionAngle1;
        public Vector4 SpotDirectionAngle2;
        public Vector4 SpotDirectionAngle3;
        public Vector4 SpotColorIntensity0;
        public Vector4 SpotColorIntensity1;
        public Vector4 SpotColorIntensity2;
        public Vector4 SpotColorIntensity3;
        public Vector4 LightCount; // x = point light count, y = position quantization resolution in pixels, z = spot light count
    }
}
