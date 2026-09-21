using System.Numerics;
using SDL;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal unsafe interface ISdlGpuPipeline : IDisposable
{
    public const int DiffuseTexture = 0;
    public const int GlowTexture = 1;
    public const int NormalTexture = 2;

    void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures, Matrix4x4 transform);
}