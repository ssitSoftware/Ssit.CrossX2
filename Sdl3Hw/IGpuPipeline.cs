using SDL;

namespace Sdl3Hw;

public unsafe interface IGpuPipeline : IDisposable
{
    void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures);
}