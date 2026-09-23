using System.Numerics;
using System.Runtime.InteropServices;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Lighting;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Pipelines;

internal static unsafe class SdlGpuLightingUniforms
{
    public static void Push(SdlGpuRenderer renderer, SDL_GPUCommandBuffer* commandBuffer)
    {
        
        var lightingManager = (LightingManager)renderer.LightingManager;

        var pointLights = lightingManager.PointLights;
        var pointCount = Math.Min(lightingManager.PointLightsCount, ILightingManager.MaxPointLights);

        Span<Vector4> positionRadius = stackalloc Vector4[ILightingManager.MaxPointLights];
        Span<Vector4> colorIntensity = stackalloc Vector4[ILightingManager.MaxPointLights];

        for (int i = 0; i < pointCount; i++)
        {
            PointLight light = pointLights[i];
            positionRadius[i] = new Vector4(light.Position.X, light.Position.Y, light.Position.Z, light.Radius);
            colorIntensity[i] = new Vector4(light.Color.Rf, light.Color.Gf, light.Color.Bf, light.Intensity);
        }

        var spotLights = lightingManager.SpotLights;
        int spotCount = Math.Min(lightingManager.SpotLightsCount, ILightingManager.MaxSpotLights);

        Span<Vector4> spotPositionRadius = stackalloc Vector4[ILightingManager.MaxSpotLights];
        Span<Vector4> spotDirectionAngle = stackalloc Vector4[ILightingManager.MaxSpotLights];
        Span<Vector4> spotColorIntensity = stackalloc Vector4[ILightingManager.MaxSpotLights];

        for (int i = 0; i < spotCount; i++)
        {
            SpotLight spot = spotLights[i];
            spotPositionRadius[i] = new Vector4(spot.Position.X, spot.Position.Y, spot.Position.Z, spot.Radius);
            spotDirectionAngle[i] = new Vector4(spot.Direction.X, spot.Direction.Y,
                MathF.Cos(spot.OuterAngle * MathF.PI / 180f), MathF.Cos(spot.InnerAngle * MathF.PI / 180f));
            spotColorIntensity[i] = new Vector4(spot.Color.Rf, spot.Color.Gf, spot.Color.Bf, spot.Intensity);
        }

        var directionalLights = lightingManager.DirectionalLights;
        int dirCount = Math.Min(lightingManager.DirectionalLightsCount, ILightingManager.MaxDirectionalLights);

        Span<Vector4> dirDirection = stackalloc Vector4[ILightingManager.MaxDirectionalLights];
        Span<Vector4> dirColorIntensity = stackalloc Vector4[ILightingManager.MaxDirectionalLights];

        for (int i = 0; i < dirCount; i++)
        {
            DirectionalLight dir = directionalLights[i];
            dirDirection[i] = new Vector4(dir.Direction.X, dir.Direction.Y, dir.Direction.Z, 0f);
            dirColorIntensity[i] = new Vector4(dir.Color.Rf, dir.Color.Gf, dir.Color.Bf, dir.Intensity);
        }

        var uniforms = new LightingUniforms
        {
            Ambient = new Vector4(lightingManager.AmbientLight.Rf, lightingManager.AmbientLight.Gf, lightingManager.AmbientLight.Bf, 0f),
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
            SpotPositionRadius4 = spotPositionRadius[4],
            SpotPositionRadius5 = spotPositionRadius[5],
            SpotPositionRadius6 = spotPositionRadius[6],
            SpotPositionRadius7 = spotPositionRadius[7],
            SpotDirectionAngle0 = spotDirectionAngle[0],
            SpotDirectionAngle1 = spotDirectionAngle[1],
            SpotDirectionAngle2 = spotDirectionAngle[2],
            SpotDirectionAngle3 = spotDirectionAngle[3],
            SpotDirectionAngle4 = spotDirectionAngle[4],
            SpotDirectionAngle5 = spotDirectionAngle[5],
            SpotDirectionAngle6 = spotDirectionAngle[6],
            SpotDirectionAngle7 = spotDirectionAngle[7],
            SpotColorIntensity0 = spotColorIntensity[0],
            SpotColorIntensity1 = spotColorIntensity[1],
            SpotColorIntensity2 = spotColorIntensity[2],
            SpotColorIntensity3 = spotColorIntensity[3],
            SpotColorIntensity4 = spotColorIntensity[4],
            SpotColorIntensity5 = spotColorIntensity[5],
            SpotColorIntensity6 = spotColorIntensity[6],
            SpotColorIntensity7 = spotColorIntensity[7],
            LightCount = new Vector4(pointCount, lightingManager.Resolution, spotCount, 0f),
            DirDirection0 = dirDirection[0],
            DirDirection1 = dirDirection[1],
            DirDirection2 = dirDirection[2],
            DirDirection3 = dirDirection[3],
            DirColorIntensity0 = dirColorIntensity[0],
            DirColorIntensity1 = dirColorIntensity[1],
            DirColorIntensity2 = dirColorIntensity[2],
            DirColorIntensity3 = dirColorIntensity[3],
            DirectionalCount = new Vector4(dirCount, 0f, 0f, 0f),
        };

        SDL_PushGPUFragmentUniformData(commandBuffer, 0, (IntPtr)(&uniforms), (uint)sizeof(LightingUniforms));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LightingUniforms
    {
        public Vector4 Ambient;
        public Vector4 LightPositionRadius0; // xyz = position, w = radius
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
        public Vector4 SpotPositionRadius0; // xyz = position, w = radius
        public Vector4 SpotPositionRadius1;
        public Vector4 SpotPositionRadius2;
        public Vector4 SpotPositionRadius3;
        public Vector4 SpotPositionRadius4;
        public Vector4 SpotPositionRadius5;
        public Vector4 SpotPositionRadius6;
        public Vector4 SpotPositionRadius7;
        public Vector4 SpotDirectionAngle0; // xy = normalized 2D direction, z = cos(outerAngle), w = cos(innerAngle)
        public Vector4 SpotDirectionAngle1;
        public Vector4 SpotDirectionAngle2;
        public Vector4 SpotDirectionAngle3;
        public Vector4 SpotDirectionAngle4;
        public Vector4 SpotDirectionAngle5;
        public Vector4 SpotDirectionAngle6;
        public Vector4 SpotDirectionAngle7;
        public Vector4 SpotColorIntensity0;
        public Vector4 SpotColorIntensity1;
        public Vector4 SpotColorIntensity2;
        public Vector4 SpotColorIntensity3;
        public Vector4 SpotColorIntensity4;
        public Vector4 SpotColorIntensity5;
        public Vector4 SpotColorIntensity6;
        public Vector4 SpotColorIntensity7;
        public Vector4 LightCount; // x = point light count, y = position quantization resolution in pixels, z = spot light count, w unused
        public Vector4 DirDirection0; // xyz = normalized 3D light-travel direction, w unused
        public Vector4 DirDirection1;
        public Vector4 DirDirection2;
        public Vector4 DirDirection3;
        public Vector4 DirColorIntensity0; // rgb = color, a = intensity
        public Vector4 DirColorIntensity1;
        public Vector4 DirColorIntensity2;
        public Vector4 DirColorIntensity3;
        public Vector4 DirectionalCount; // x = directional light count, yzw unused
    }
}
