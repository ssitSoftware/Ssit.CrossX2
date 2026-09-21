using System.Numerics;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Pipelines;

internal sealed unsafe class SdlGpuTextureBumpLightingPipeline : SdlGpuTexturePipeline
{
    public SdlGpuTextureBumpLightingPipeline(SdlGpuRenderer renderer)
        : base(renderer,
            "Pipelines.TextureLightBump.vert",
            "Pipelines.TextureLightBump.frag",
            fragmentUniformBuffers: 1, fragmentSamplers: 2)
    {
    }

    public override void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures, Matrix4x4 transform)
    {
        base.Bind(commandBuffer, renderPass, textures, transform);

        var sampler = GpuRenderer.RenderStateProvider.TextureFilter == TextureFilter.Point ? PointSampler : LinearSampler;

        var normalBinding = stackalloc SDL_GPUTextureSamplerBinding[1];
        normalBinding[0] = new SDL_GPUTextureSamplerBinding { texture = textures[ISdlGpuPipeline.NormalTexture], sampler = sampler };
        SDL_BindGPUFragmentSamplers(renderPass, 1, normalBinding, 1);

        SdlGpuLightingUniforms.Push(GpuRenderer, commandBuffer);
    }
}
