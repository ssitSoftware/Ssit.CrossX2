using SDL;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Services;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal unsafe class SdlGpuRenderTarget : IRenderTarget, ISdlGpuTexture
{
    public static int Count { get; private set; }

    private readonly SdlGpuRenderer _renderer;
    private readonly IActionScheduler _actionScheduler;
    private readonly SDL_GPUDevice* _device;
    private bool _disposed;

    // The content rendered into the target is used as-is in every pass: in the glow pass it is the glow content
    // (e.g. an offscreen container rendered in the glow pass), so it's the glow map as well.
    public TextureMaps Maps => TextureMaps.Diffuse | TextureMaps.Glow;

    public Size Size { get; }

    public SDL_GPUTextureFormat Format { get; }
    
    public SDL_GPUTexture* GetMap(TextureMaps maps)
    {
        if(maps is TextureMaps.Diffuse or TextureMaps.Glow)
            return Handle;

        return null;
    }

    public SDL_GPUTexture* Handle { get; private set; }

    public SdlGpuRenderTarget(SdlGpuRenderer renderer, IActionScheduler actionScheduler, CreateRenderTargetParameters parameters)
    {
        _renderer = renderer;
        _actionScheduler = actionScheduler;
        //renderer.SubmitCommandBuffer();

        _device = renderer.Device;
        Size = parameters.Size;
        Format = SDL_GetGPUSwapchainTextureFormat(renderer.Device, renderer.Window);

        var createInfo = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = Format,
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

        Count++;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        var handle = Handle;
        Handle = null;
        _disposed = true;
        Count--;

        _actionScheduler.Schedule(() =>
        {
            SDL_ReleaseGPUTexture(_device, handle);
        });
    }
}