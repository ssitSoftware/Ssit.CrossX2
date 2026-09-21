using SDL;
using Ssit.CrossX2.Framework.Graphics;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal unsafe class SdlGpuRenderTarget : IRenderTarget, ISdlGpuTexture
{
    private readonly SDL_GPUDevice* _device;
    private bool _disposed;

    public TextureMaps Maps => TextureMaps.Diffuse;

    public Size Size { get; }
    
    public SDL_GPUTexture* GetMap(TextureMaps maps)
    {
        if(maps == TextureMaps.Diffuse)
            return Handle;

        return null;
    }

    public SDL_GPUTexture* Handle { get; private set; }

    public SdlGpuRenderTarget(SdlHandles handles, CreateRenderTargetParameters parameters)
    {
        _device = handles.GpuDevice;
        Size = parameters.Size;

        var createInfo = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = SDL_GetGPUSwapchainTextureFormat(handles.GpuDevice, handles.Window),
            usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET | SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER,
            width = (uint)Size.Width,
            height = (uint)Size.Height,
            layer_count_or_depth = 1,
            num_levels = 1,
            sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
        };

        Handle = SDL_CreateGPUTexture(_device, &createInfo);

        if (Handle == null)
            throw new InvalidOperationException($"SDL_CreateGPUTexture failed: {SDL_GetError()}");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        SDL_ReleaseGPUTexture(_device, Handle);
        Handle = null;
        _disposed = true;
    }
}