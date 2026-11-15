using SDL;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal unsafe interface ISdlGpuTexture: ITexture
{
    SDL_GPUTexture* GetMap(TextureMaps maps);

    /// <summary>
    /// Pixel format of the GPU texture(s).
    /// </summary>
    SDL_GPUTextureFormat Format { get; }
}