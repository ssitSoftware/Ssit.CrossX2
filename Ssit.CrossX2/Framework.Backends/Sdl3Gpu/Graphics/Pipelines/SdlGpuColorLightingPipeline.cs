using System.Numerics;
using SDL;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Pipelines;

internal sealed unsafe class ColorLightingPipeline : SdlGpuColorPipeline
{
    public ColorLightingPipeline(SdlHandles handles, SdlGpuRenderer gpuRenderer, SDL_GPUPrimitiveType primitiveType)
        : base(handles, gpuRenderer, primitiveType,
            "Pipelines.ColorLight.vert",
            "Pipelines.ColorLight.frag", fragmentUniformBuffers: 1)
    {
    }

    public override void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures, Matrix4x4 transform)
    {
        base.Bind(commandBuffer, renderPass, textures, transform);

        SdlGpuLightingUniforms.Push(GpuRenderer, commandBuffer);
    }
}
