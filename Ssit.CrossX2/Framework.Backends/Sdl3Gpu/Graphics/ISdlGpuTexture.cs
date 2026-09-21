using SDL;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal unsafe interface ISdlGpuTexture: ITexture
{
    SDL_GPUTexture* GetMap(TextureMaps maps);
}