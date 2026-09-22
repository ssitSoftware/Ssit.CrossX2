using System.Numerics;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Utils;
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

    private readonly int _strideBytes = sizeof(VertexPcttb);

    private SDL_GPUBuffer* _gpuBuffer;
    
    private readonly VertexPcttb[] _buffer = new VertexPcttb[BufferVerticesCount];
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

        _buffer[_currentPosition++] = p1;
        _buffer[_currentPosition++] = p2;
    }

    public void PushTriangle(VertexPct p1, VertexPct p2, VertexPct p3, ITexture texture)
    {
        CheckBufferOverflow(3);

        var primitiveType = texture != null && texture.Maps.HasFlag(TextureMaps.Normal)
            ? PrimitiveType.TrianglesWithTangents
            : PrimitiveType.Triangles;

        if (_currentTexture != texture || _currentPrimitiveType != primitiveType)
        {
            StoreCommand();
            _currentTexture = texture;
            _currentPrimitiveType = primitiveType;
        }
        
        if (primitiveType == PrimitiveType.TrianglesWithTangents)
        {
            var (tangent, bitangent) = GeometryUtils.CalculateTangentAndBiTangent(
                new Vector2(p1.Position.X, p1.Position.Y),
                new Vector2(p2.Position.X, p2.Position.Y),
                new Vector2(p3.Position.X, p3.Position.Y),
                p1.TexCoordinates,
                p2.TexCoordinates,
                p3.TexCoordinates);
            
            _buffer[_currentPosition++] = new VertexPcttb(p1, tangent, bitangent);
            _buffer[_currentPosition++] = new VertexPcttb(p2, tangent, bitangent);
            _buffer[_currentPosition++] = new VertexPcttb(p3, tangent, bitangent);
        }
        else
        {
            _buffer[_currentPosition++] = p1;
            _buffer[_currentPosition++] = p2;
            _buffer[_currentPosition++] = p3;
        }
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
            _renderer.SubmitCommandBuffer();
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
        
        _currentPosition = 0;
        _startPosition = 0;
        _currentTexture = null;
    }

    private void RenderQueueElements()
    {
        if (_currentPosition == 0)
            return;

        _renderer.EndCurrentGpuRenderPass(false);
        
        var device = _renderer.Device;
        var commandBuffer = _renderer.CommandBuffer;

        uint byteSize = (uint)(_currentPosition * _strideBytes);

        var transferBufferCreateInfo = new SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = byteSize,
        };
        SDL_GPUTransferBuffer* transferBuffer = SDL_CreateGPUTransferBuffer(device, &transferBufferCreateInfo);

        if (transferBuffer == null)
            throw new InvalidOperationException($"SDL_CreateGPUTransferBuffer failed: {SDL_GetError()}");

        void* mapped = (void*)SDL_MapGPUTransferBuffer(device, transferBuffer, false);
        new ReadOnlySpan<VertexPcttb>(_buffer, 0, _currentPosition).CopyTo(new Span<VertexPcttb>(mapped, _currentPosition));
        SDL_UnmapGPUTransferBuffer(device, transferBuffer);

        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

        var transferBufferLocation = new SDL_GPUTransferBufferLocation
        {
            transfer_buffer = transferBuffer,
            offset = 0,
        };
        var bufferRegion = new SDL_GPUBufferRegion
        {
            buffer = _gpuBuffer,
            offset = 0,
            size = byteSize,
        };
        SDL_UploadToGPUBuffer(copyPass, &transferBufferLocation, &bufferRegion, false);

        SDL_EndGPUCopyPass(copyPass);
        SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

        foreach (var command in _commands)
        {
            _primitiveRenderer.RenderVertices(command.Type, VertexPcttb.Components, _gpuBuffer, command.Start, command.Count, command.Texture);
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

        _disposed = true;
    }
}