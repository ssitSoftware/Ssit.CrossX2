using SDL;

namespace Ssit.CrossX2._Sdl3Impl.Graphics.Pipelines;

internal sealed unsafe class ColorLightingPipeline : SdlGpuColorPipeline
{
    public ColorLightingPipeline(SdlHandles handles, SdlGpuRenderer gpuRenderer, SDL_GPUPrimitiveType primitiveType)
        : base(handles, gpuRenderer, primitiveType,
            "Pipelines.Shaders.ColorLight.vert",
            "Pipelines.Shaders.ColorLight.frag", fragmentUniformBuffers: 1)
    {
    }

    public override void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures)
    {
        base.Bind(commandBuffer, renderPass, textures);

        SdlGpuLightingUniforms.Push(GpuRenderer, commandBuffer);
    }
}
