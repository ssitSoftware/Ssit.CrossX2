using System.Numerics;
using SDL;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Pipelines;

internal sealed unsafe class SdlGpuTextureLightingPipeline : SdlGpuTexturePipeline
{
    public SdlGpuTextureLightingPipeline(SdlGpuRenderer renderer)
        : base(renderer,
            "Pipelines.TextureLight.vert",
            "Pipelines.TextureLight.frag", fragmentUniformBuffers: 1)
    {
    }

    public override void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures, Matrix4x4 transform)
    {
        base.Bind(commandBuffer, renderPass, textures, transform);

        SdlGpuLightingUniforms.Push(GpuRenderer, commandBuffer);
    }
}
