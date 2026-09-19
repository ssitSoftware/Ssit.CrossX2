using SDL;
using Ssit.CrossX2.Graphics;

namespace Ssit.CrossX2._Sdl3Impl.Graphics;

public unsafe interface ISdlGpuPipeline : IDisposable
{
    void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures);
}