using SDL;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Misc;
using Ssit.CrossX2.Framework.Services;
using static SDL.SDL3;
using static SDL.SDL3_image;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal unsafe class SdlGpuTexture: ISdlGpuTexture
{
    public static int Count { get; private set; }
    
    private readonly SDL_GPUDevice* _device;
    private readonly IActionScheduler _actionScheduler;
    private bool _disposed;
    
    private SDL_GPUTexture* _diffuse = null;
    private SDL_GPUTexture* _glow = null;
    private SDL_GPUTexture* _light = null;
    private SDL_GPUTexture* _collisionMask = null;

    private readonly bool _glowFromDiffuse;
    private readonly bool _glowFromLight;
    
    public SdlGpuTexture(SdlHandles handles, IActionScheduler actionScheduler, LoadTextureParameters parameters)
    {
        _device = handles.GpuDevice;
        _actionScheduler = actionScheduler;
        TextureMaps maps = 0; 
        
        Size? size = null;

        _glowFromDiffuse = parameters.GlowFromDiffuse;
        _glowFromLight = parameters.GlowFromLightMap;
        
        if (parameters.DiffuseMapStream is not null)
        {
            var (tex, ts) = LoadTextureFromStream(parameters.DiffuseMapStream, true);
            size = ts;

            _diffuse = tex.Pointer;
            
            maps |= TextureMaps.Diffuse;
        }
        
        if (parameters.GlowMapStream is not null)
        {
            var (tex, ts) = LoadTextureFromStream(parameters.GlowMapStream, true);
            size ??= ts;
            
            _glow = tex.Pointer;
            
            maps |= TextureMaps.Glow;
        }
        
        if (parameters.LightMapStream is not null)
        {
            var (tex, ts) = LoadTextureFromStream(parameters.LightMapStream, true);
            size ??= ts;

            _light = tex.Pointer;
            maps |= TextureMaps.Light;
        }

        if (parameters.CollisionMaskStream is not null)
        {
            var (tex, ts) = LoadTextureFromStream(parameters.CollisionMaskStream, true);
            size ??= ts;

            _collisionMask = tex.Pointer;
            maps |= TextureMaps.CollisionMask;
        }

        if (_glowFromDiffuse && _diffuse != null)
        {
            maps |= TextureMaps.Glow;
        }
        
        Size = size.GetValueOrDefault();
        Maps = maps;

        Count++;
    }

    private (SdlHandle<SDL_GPUTexture>, Size) LoadTextureFromStream(Stream stream, bool premultiply = false)
    {
        byte[] bytes;

        if (stream is MemoryStream ms)
        {
            bytes = ms.ToArray();
        }
        else
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            bytes = buffer.ToArray();
        }

        SDL_Surface* surface;

        fixed (byte* bytesPtr = bytes)
        {
            SDL_IOStream* io = SDL_IOFromConstMem((IntPtr)bytesPtr, (nuint)bytes.Length);
            surface = IMG_Load_IO(io, true);
        }

        if (surface == null)
            throw new InvalidOperationException($"IMG_Load_IO failed: {SDL_GetError()}");

        if (surface->format != SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888)
        {
            SDL_Surface* converted = SDL_ConvertSurface(surface, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888);
            SDL_DestroySurface(surface);

            if (converted == null)
                throw new InvalidOperationException($"SDL_ConvertSurface failed: {SDL_GetError()}");

            surface = converted;
        }

        if (premultiply && !SDL_PremultiplySurfaceAlpha(surface, false))
        {
            SDL_DestroySurface(surface);
            throw new InvalidOperationException($"SDL_PremultiplySurfaceAlpha failed: {SDL_GetError()}");
        }

        int width = surface->w;
        int height = surface->h;
        uint rowBytes = (uint)width * 4;
        uint dataSize = rowBytes * (uint)height;

        var textureCreateInfo = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
            usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER,
            width = (uint)width,
            height = (uint)height,
            layer_count_or_depth = 1,
            num_levels = 1,
            sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
        };
        SDL_GPUTexture* texture = SDL_CreateGPUTexture(_device, &textureCreateInfo);

        if (texture == null)
        {
            SDL_DestroySurface(surface);
            throw new InvalidOperationException($"SDL_CreateGPUTexture failed: {SDL_GetError()}");
        }

        var transferBufferCreateInfo = new SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = dataSize,
        };
        SDL_GPUTransferBuffer* transferBuffer = SDL_CreateGPUTransferBuffer(_device, &transferBufferCreateInfo);

        if (transferBuffer == null)
        {
            SDL_ReleaseGPUTexture(_device, texture);
            SDL_DestroySurface(surface);
            throw new InvalidOperationException($"SDL_CreateGPUTransferBuffer failed: {SDL_GetError()}");
        }

        byte* mapped = (byte*)SDL_MapGPUTransferBuffer(_device, transferBuffer, false);
        if (mapped == null)
        {
            SDL_ReleaseGPUTransferBuffer(_device, transferBuffer);
            SDL_ReleaseGPUTexture(_device, texture);
            SDL_DestroySurface(surface);
            throw new InvalidOperationException($"SDL_MapGPUTransferBuffer failed: {SDL_GetError()}");
        }

        byte* srcBase = (byte*)surface->pixels;

        for (int row = 0; row < height; row++)
        {
            Buffer.MemoryCopy(srcBase + row * surface->pitch, mapped + row * rowBytes, rowBytes, rowBytes);
        }

        SDL_UnmapGPUTransferBuffer(_device, transferBuffer);
        SDL_DestroySurface(surface);

        // Content may be loaded from worker threads (button commands / await continuations).
        // Recording + submitting a copy pass there races the main thread's frame while it
        // holds a swapchain drawable (e.g. during a fullscreen transition) and can hang the GPU.
        // Queue order guarantees this runs before the next Render and before any scheduled release.
        var device = _device;
        _actionScheduler.ExecuteOnMainThread(() => Upload(device, texture, transferBuffer, (uint)width, (uint)height));

        return (new(texture), new Size(width, height));
    }

    private static void Upload(SDL_GPUDevice* device, SDL_GPUTexture* texture, SDL_GPUTransferBuffer* transferBuffer, uint width, uint height)
    {
        SDL_GPUCommandBuffer* commandBuffer = SDL_AcquireGPUCommandBuffer(device);
        if (commandBuffer == null)
        {
            Console.Error.WriteLine($"Texture upload skipped, SDL_AcquireGPUCommandBuffer failed: {SDL_GetError()}");
            SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
            return;
        }

        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

        var transferInfo = new SDL_GPUTextureTransferInfo
        {
            transfer_buffer = transferBuffer,
            offset = 0,
            pixels_per_row = width,
            rows_per_layer = height,
        };
        var region = new SDL_GPUTextureRegion
        {
            texture = texture,
            mip_level = 0,
            layer = 0,
            x = 0,
            y = 0,
            z = 0,
            w = width,
            h = height,
            d = 1,
        };
        SDL_UploadToGPUTexture(copyPass, &transferInfo, &region, false);

        SDL_EndGPUCopyPass(copyPass);
        SDL_SubmitGPUCommandBuffer(commandBuffer);

        SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;

        var device = _device;
        var diffuse = _diffuse;
        var glow = _glow;
        var light = _light;
        var collisionMask = _collisionMask;
        _diffuse = null;
        _glow = null;
        _light = null;
        _collisionMask = null;

        // Release on the main thread, after any pending Upload queued by the constructor.
        _actionScheduler.Schedule(() =>
        {
            if (diffuse != null) SDL_ReleaseGPUTexture(device, diffuse);
            if (glow != null) SDL_ReleaseGPUTexture(device, glow);
            if (light != null) SDL_ReleaseGPUTexture(device, light);
            if (collisionMask != null) SDL_ReleaseGPUTexture(device, collisionMask);
        });

        _disposed = true;
        Count--;
    }

    public TextureMaps Maps { get; }
    public Size Size { get; }

    // All maps are loaded as R8G8B8A8 (see LoadTextureFromStream).
    public SDL_GPUTextureFormat Format => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;
    
    public SDL_GPUTexture* GetMap(TextureMaps maps)
    {
        switch (maps)
        {
            case TextureMaps.Diffuse:
                return _diffuse;
            case TextureMaps.Glow:
                return _glow != null ? _glow : _glowFromDiffuse ? _diffuse : _glowFromLight ? _light : null;
            case TextureMaps.Light:
                return _light;
            case TextureMaps.CollisionMask:
                return _collisionMask;
            default:
                return null;
        }
    }
}