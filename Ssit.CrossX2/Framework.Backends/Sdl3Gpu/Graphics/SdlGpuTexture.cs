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
            var (tex, ts) = LoadTextureFromStream(parameters.DiffuseMapStream);
            size = ts;

            _diffuse = tex.Pointer;
            
            maps |= TextureMaps.Diffuse;
        }
        
        if (parameters.GlowMapStream is not null)
        {
            var (tex, ts) = LoadTextureFromStream(parameters.GlowMapStream);
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

    private (SdlHandle<SDL_GPUTexture>, Size) LoadTextureFromStream(Stream stream)
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

        SDL_GPUCommandBuffer* commandBuffer = SDL_AcquireGPUCommandBuffer(_device);
        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

        SDL_GPUTexture* texture;
        int width, height;
        
        fixed (byte* bytesPtr = bytes)
        {
            SDL_IOStream* io = SDL_IOFromConstMem((IntPtr)bytesPtr, (nuint)bytes.Length);
            texture = IMG_LoadGPUTexture_IO(_device, copyPass, io, true, &width, &height);
        }

        SDL_EndGPUCopyPass(copyPass);
        SDL_SubmitGPUCommandBuffer(commandBuffer);

        if (texture == null)
            throw new InvalidOperationException($"IMG_LoadGPUTexture_IO failed: {SDL_GetError()}");

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