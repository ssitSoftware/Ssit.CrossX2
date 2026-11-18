using System.Numerics;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Renderers;

internal unsafe class SdlGpuRenderQueue: IDisposable, IRenderQueueInternal
{
    struct Command
    {
        public PrimitiveType Type;
        public ITexture Texture;
        public int Start;
        public int Count;
    }
    
    private const int BufferVerticesCount = ushort.MaxValue;

    private readonly int _strideBytes = sizeof(VertexPct);

    private SDL_GPUBuffer* _gpuBuffer;
    private SDL_GPUTransferBuffer* _transferBuffer;

    // Vertices before this index are already in _gpuBuffer (see RenderQueueElements).
    private int _uploadedPosition;

    private readonly VertexPct[] _buffer = new VertexPct[BufferVerticesCount];
    private readonly List<Command> _commands = new();
    
    private int _currentPosition;
    private ITexture _currentTexture;

    private int _startPosition;
    private readonly SdlGpuRenderer _renderer;
    private readonly SdlGpuPrimitiveRenderer _primitiveRenderer;

    private PrimitiveType _currentPrimitiveType = PrimitiveType.Lines;
    private bool _disposed;

    public SdlGpuRenderQueue(SdlGpuRenderer renderer)
    {
        _renderer = renderer;
        _primitiveRenderer  = renderer.PrimitiveRenderer;

        var createInfo = new SDL_GPUBufferCreateInfo
        {
            usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX,
            size = (uint)(_strideBytes * BufferVerticesCount),
        };
        _gpuBuffer = SDL_CreateGPUBuffer(_renderer.Device, &createInfo);

        if (_gpuBuffer == null)
            throw new InvalidOperationException($"SDL_CreateGPUBuffer failed: {SDL_GetError()}");

        // One persistent upload buffer instead of creating and releasing a D3D12 resource on every flush.
        var transferBufferCreateInfo = new SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = createInfo.size,
        };
        _transferBuffer = SDL_CreateGPUTransferBuffer(_renderer.Device, &transferBufferCreateInfo);

        if (_transferBuffer == null)
            throw new InvalidOperationException($"SDL_CreateGPUTransferBuffer failed: {SDL_GetError()}");
    }

    public void PushLine(VertexPct p1, VertexPct p2)
    {
        CheckBufferOverflow(2);

        if (_currentTexture != null || _currentPrimitiveType != PrimitiveType.Lines)
        {
            StoreCommand();
            _currentPrimitiveType = PrimitiveType.Lines;
            _currentTexture = null;
        }
        
        var tint = _renderer.RenderStateProvider.TintColor;
        if (tint.A == 0)
            return;
        
        p1.Color *= tint;
        p2.Color *= tint;
        
        _buffer[_currentPosition++] = p1;
        _buffer[_currentPosition++] = p2;
    }

    public void PushTriangle(VertexPct p1, VertexPct p2, VertexPct p3, ITexture texture)
    {
        CheckBufferOverflow(3);

        if (_currentTexture != texture || _currentPrimitiveType != PrimitiveType.Triangles)
        {
            StoreCommand();
            _currentTexture = texture;
            _currentPrimitiveType = PrimitiveType.Triangles;
        }

        var tint = _renderer.RenderStateProvider.TintColor;
        if (tint.A == 0)
            return;

        p1.Color *= tint;
        p2.Color *= tint;
        p3.Color *= tint;

        _buffer[_currentPosition++] = p1;
        _buffer[_currentPosition++] = p2;
        _buffer[_currentPosition++] = p3;
    }

    public void PushVertices(PrimitiveType type, IVertexBuffer vertices, int start, int count, ITexture texture = null)
    {
        Flush();
        _renderer.PrimitiveRenderer.RenderVertices(type, vertices, start, count, texture);
    }

    private void CheckBufferOverflow(int count)
    {
        if (_currentPosition + count >= BufferVerticesCount)
        {
            // Draw what is queued, then wrap to the start. The next upload cycles both buffers, so draws
            // already recorded (or still running on the GPU) keep reading the old contents. No submit needed.
            Flush();
            _currentPosition = 0;
            _startPosition = 0;
            _uploadedPosition = 0;
        }
    }

    private void StoreCommand()
    {
        if (_currentPosition == _startPosition)
            return;
        
        _commands.Add(new Command
        {
            Texture = _currentTexture,
            Start = _startPosition,
            Count = _currentPosition - _startPosition,
            Type = _currentPrimitiveType
        });
        
        _startPosition = _currentPosition;
        _currentTexture = null;
    }

    public void Flush()
    {
        StoreCommand();
        RenderQueueElements();

        _commands.Clear();

        // Positions are not reset: the next batch is appended after this one, so its upload never
        // overwrites vertices that draws recorded earlier still read. Wrapping happens in CheckBufferOverflow.
        _startPosition = _currentPosition;
        _currentTexture = null;
    }

    /// <summary>Batches uploaded and drawn since the counter was last reset (perf stats).</summary>
    internal static int FlushCount;

    private void RenderQueueElements()
    {
        if (_currentPosition == _uploadedPosition)
            return;

        FlushCount++;

        _renderer.EndCurrentGpuRenderPass(false);

        var device = _renderer.Device;
        var commandBuffer = _renderer.CommandBuffer;

        var first = _uploadedPosition;
        var count = _currentPosition - first;
        var byteOffset = (uint)(first * _strideBytes);
        var byteSize = (uint)(count * _strideBytes);

        // Cycle when starting from the beginning: earlier frames' copies/draws may still use the old contents,
        // and SDL then hands out another internal buffer instead of stalling. Appended ranges never overlap
        // what was uploaded before, so they don't need it.
        var cycle = first == 0;

        void* mapped = (void*)SDL_MapGPUTransferBuffer(device, _transferBuffer, cycle);

        if (mapped == null)
            throw new InvalidOperationException($"SDL_MapGPUTransferBuffer failed: {SDL_GetError()}");

        new ReadOnlySpan<VertexPct>(_buffer, first, count).CopyTo(new Span<VertexPct>((byte*)mapped + byteOffset, count));
        SDL_UnmapGPUTransferBuffer(device, _transferBuffer);

        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

        var transferBufferLocation = new SDL_GPUTransferBufferLocation
        {
            transfer_buffer = _transferBuffer,
            offset = byteOffset,
        };
        var bufferRegion = new SDL_GPUBufferRegion
        {
            buffer = _gpuBuffer,
            offset = byteOffset,
            size = byteSize,
        };
        SDL_UploadToGPUBuffer(copyPass, &transferBufferLocation, &bufferRegion, cycle);

        SDL_EndGPUCopyPass(copyPass);

        _uploadedPosition = _currentPosition;

        foreach (var command in _commands)
        {
            _primitiveRenderer.RenderVertices(command.Type, VertexPct.Components, _gpuBuffer, command.Start, command.Count, command.Texture);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_gpuBuffer != null)
        {
            SDL_ReleaseGPUBuffer(_renderer.Device, _gpuBuffer);
            _gpuBuffer = null;
        }

        if (_transferBuffer != null)
        {
            SDL_ReleaseGPUTransferBuffer(_renderer.Device, _transferBuffer);
            _transferBuffer = null;
        }

        _disposed = true;
    }
}