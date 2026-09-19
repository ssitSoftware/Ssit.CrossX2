using SDL;

namespace Ssit.CrossX2._Sdl3Impl.Graphics.Pipelines;

internal sealed unsafe class SdlGpuTextureLightingPipeline : SdlGpuTexturePipeline
{
    public SdlGpuTextureLightingPipeline(SdlGpuRenderer renderer)
        : base(renderer,
            "Pipelines.Shaders.TextureLight.vert",
            "Pipelines.Shaders.TextureLight.frag", fragmentUniformBuffers: 1)
    {
    }

    public override void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures)
    {
        base.Bind(commandBuffer, renderPass, textures);

        SdlGpuLightingUniforms.Push(GpuRenderer, commandBuffer);
    }
}
