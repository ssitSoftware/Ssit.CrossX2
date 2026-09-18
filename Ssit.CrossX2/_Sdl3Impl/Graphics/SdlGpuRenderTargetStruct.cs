using SDL;

namespace Ssit.CrossX2._Sdl3Impl.Graphics;

internal readonly unsafe struct SdlGpuRenderTargetStruct(SDL_GPUTexture* texture, Size size)
{
    public static readonly SdlGpuRenderTargetStruct Null = new SdlGpuRenderTargetStruct(null, Size.Zero);
    
    public SDL_GPUTexture* Handle { get; } = texture;
    public Size Size { get; } = size;
}