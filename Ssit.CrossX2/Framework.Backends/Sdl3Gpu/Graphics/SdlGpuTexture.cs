using SDL;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Misc;
using static SDL.SDL3;
using static SDL.SDL3_image;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal unsafe class SdlGpuTexture: ISdlGpuTexture
{
    private readonly SDL_GPUDevice* _device;
    private bool _disposed;
    
    private SDL_GPUTexture* _diffuse = null;
    private SDL_GPUTexture* _glow = null;
    private SDL_GPUTexture* _normal = null;
    
    public SdlGpuTexture(SdlHandles handles, LoadTextureParameters parameters)
    {
        _device = handles.GpuDevice;
        TextureMaps maps = 0; 
        
        Size? size = null;
        
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
        
        if (parameters.NormalMapStream is not null)
        {
            var (tex, ts) = LoadTextureFromStream(parameters.NormalMapStream);
            size ??= ts;
            
            _normal = tex.Pointer;
            maps |= TextureMaps.Normal;
        }

        Size = size.GetValueOrDefault();
        Maps = maps;
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
        byte* srcBase = (byte*)surface->pixels;

        for (int row = 0; row < height; row++)
        {
            Buffer.MemoryCopy(srcBase + row * surface->pitch, mapped + row * rowBytes, rowBytes, rowBytes);
        }

        SDL_UnmapGPUTransferBuffer(_device, transferBuffer);

        SDL_GPUCommandBuffer* commandBuffer = SDL_AcquireGPUCommandBuffer(_device);
        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

        var transferInfo = new SDL_GPUTextureTransferInfo
        {
            transfer_buffer = transferBuffer,
            offset = 0,
            pixels_per_row = (uint)width,
            rows_per_layer = (uint)height,
        };
        var region = new SDL_GPUTextureRegion
        {
            texture = texture,
            mip_level = 0,
            layer = 0,
            x = 0,
            y = 0,
            z = 0,
            w = (uint)width,
            h = (uint)height,
            d = 1,
        };
        SDL_UploadToGPUTexture(copyPass, &transferInfo, &region, false);

        SDL_EndGPUCopyPass(copyPass);
        SDL_SubmitGPUCommandBuffer(commandBuffer);

        SDL_ReleaseGPUTransferBuffer(_device, transferBuffer);
        SDL_DestroySurface(surface);

        return (new(texture), new Size(width, height));
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_diffuse != null)
        {
            SDL_ReleaseGPUTexture(_device, _diffuse);
            _diffuse = null;
        }
        
        if (_glow != null)
        {
            SDL_ReleaseGPUTexture(_device, _glow);
            _glow = null;
        }
        
        if (_normal != null)
        {
            SDL_ReleaseGPUTexture(_device, _normal);
            _normal = null;
        }
        
        _disposed = true;
    }

    public TextureMaps Maps { get; }
    public Size Size { get; }
    
    public SDL_GPUTexture* GetMap(TextureMaps maps)
    {
        switch (maps)
        {
            case TextureMaps.Diffuse:
                return _diffuse;
            case TextureMaps.Glow:
                return _glow;
            case TextureMaps.Normal:
                return _normal;
            default:
                return null;
        }
    }
}