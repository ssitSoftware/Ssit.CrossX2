using System.Numerics;
using SDL;
using Ssit.CrossX2.Graphics;

using static SDL.SDL3;

namespace Ssit.CrossX2._Sdl3Impl.Graphics;

public sealed unsafe class SdlGpuVertexBuffer : IVertexBuffer
{
    private readonly SDL_GPUDevice* _device;
    private readonly int _strideBytes;

    private SDL_GPUBuffer* _buffer;
    private bool _disposed;

    public VertexComponents Components { get; }
    public int Count { get; }

    internal SDL_GPUBuffer* Handle => _buffer;

    public SdlGpuVertexBuffer(SdlHandles handles, CreateVertexBufferParameters parameters)
    {
        _device = handles.GpuDevice;
        Components = parameters.Components;
        Count = parameters.Count;
        _strideBytes = GetStrideBytes(parameters.Components);

        var createInfo = new SDL_GPUBufferCreateInfo
        {
            usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX,
            size = (uint)(Count * _strideBytes),
        };
        _buffer = SDL_CreateGPUBuffer(_device, &createInfo);

        if (_buffer == null)
            throw new InvalidOperationException($"SDL_CreateGPUBuffer failed: {SDL_GetError()}");
    }

    private static int GetStrideBytes(VertexComponents components)
    {
        int stride = 0;

        if (components.HasFlag(VertexComponents.Position))
            stride += sizeof(Vector2);

        if (components.HasFlag(VertexComponents.Color))
            stride += sizeof(RgbaColor);

        if (components.HasFlag(VertexComponents.Texture))
            stride += sizeof(Vector2);

        return stride;
    }

    public void SetData<TVertex>(TVertex[] data) where TVertex : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (data.Length > Count)
            throw new ArgumentException($"data contains {data.Length} vertices, which exceeds the buffer capacity of {Count}", nameof(data));

        if (sizeof(TVertex) != _strideBytes)
            throw new ArgumentException($"TVertex is {sizeof(TVertex)} bytes, which does not match the {_strideBytes}-byte stride implied by {Components}", nameof(data));

        if (data.Length == 0)
            return;

        uint byteSize = (uint)(data.Length * _strideBytes);

        var transferBufferCreateInfo = new SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = byteSize,
        };
        SDL_GPUTransferBuffer* transferBuffer = SDL_CreateGPUTransferBuffer(_device, &transferBufferCreateInfo);

        if (transferBuffer == null)
            throw new InvalidOperationException($"SDL_CreateGPUTransferBuffer failed: {SDL_GetError()}");

        void* mapped = (void*)SDL_MapGPUTransferBuffer(_device, transferBuffer, false);
        new ReadOnlySpan<TVertex>(data).CopyTo(new Span<TVertex>(mapped, data.Length));
        SDL_UnmapGPUTransferBuffer(_device, transferBuffer);

        SDL_GPUCommandBuffer* commandBuffer = SDL_AcquireGPUCommandBuffer(_device);
        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

        var transferBufferLocation = new SDL_GPUTransferBufferLocation
        {
            transfer_buffer = transferBuffer,
            offset = 0,
        };
        var bufferRegion = new SDL_GPUBufferRegion
        {
            buffer = _buffer,
            offset = 0,
            size = byteSize,
        };
        SDL_UploadToGPUBuffer(copyPass, &transferBufferLocation, &bufferRegion, false);

        SDL_EndGPUCopyPass(copyPass);
        SDL_SubmitGPUCommandBuffer(commandBuffer);
        SDL_ReleaseGPUTransferBuffer(_device, transferBuffer);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_buffer != null)
        {
            SDL_ReleaseGPUBuffer(_device, _buffer);
            _buffer = null;
        }

        _disposed = true;
    }
}