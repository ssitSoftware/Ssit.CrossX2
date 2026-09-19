using SDL;

namespace Ssit.CrossX2._Sdl3Impl.Graphics;

public unsafe interface ISdlGpuPipeline : IDisposable
{
    public const int DiffuseTexture = 0;
    public const int GlowTexture = 1;
    public const int NormalTexture = 2;

    void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures);
}