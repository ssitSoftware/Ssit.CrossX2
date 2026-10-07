using System.Numerics;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Pipelines;

internal sealed unsafe class SdlGpuTextureBumpLightingPipeline : SdlGpuTexturePipeline
{
    private readonly SDL_GPUTexture* _fallbackLight;

    public SdlGpuTextureBumpLightingPipeline(SdlGpuRenderer renderer)
        : base(renderer,
            "Pipelines.TextureLightBump.vert",
            "Pipelines.TextureLightBump.frag",
            fragmentUniformBuffers: 1, fragmentSamplers: 3)
    {
        _fallbackLight = GpuTextureHelper.CreateSolidColorTexture(_device, 0, 0, 0, 0);
    }

    public override void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures, Matrix4x4 transform)
    {
        base.Bind(commandBuffer, renderPass, textures, transform);

        var sampler = GpuRenderer.RenderStateProvider.TextureFilter == TextureFilter.Point ? PointSampler : LinearSampler;

        var normalBinding = stackalloc SDL_GPUTextureSamplerBinding[1];
        normalBinding[0] = new SDL_GPUTextureSamplerBinding { texture = textures[ISdlGpuPipeline.NormalTexture], sampler = sampler };
        SDL_BindGPUFragmentSamplers(renderPass, 1, normalBinding, 1);

        var lightTexture = textures.Length > ISdlGpuPipeline.LightTexture ? textures[ISdlGpuPipeline.LightTexture] : null;

        var lightBinding = stackalloc SDL_GPUTextureSamplerBinding[1];
        lightBinding[0] = new SDL_GPUTextureSamplerBinding { texture = lightTexture != null ? lightTexture : _fallbackLight, sampler = sampler };
        SDL_BindGPUFragmentSamplers(renderPass, 2, lightBinding, 1);

        SdlGpuLightingUniforms.Push(GpuRenderer, commandBuffer);
    }

    public override void Dispose()
    {
        base.Dispose();
        SDL_ReleaseGPUTexture(_device, _fallbackLight);
    }
}
