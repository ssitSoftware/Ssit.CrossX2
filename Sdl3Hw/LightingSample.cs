using System.Numerics;
using System.Reflection;
using CrossX2;
using CrossX2.Graphics.Pipelines;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_image;

namespace Sdl3Hw;

public sealed unsafe class LightingSample : ISample
{
    private const float QuadLeft = 0f;
    private const float QuadTop = 0f;
    private const float QuadRight = QuadLeft + 1000f;
    private const float QuadBottom = QuadTop + 1000f;

    private static readonly Vector2 QuadCenter = new((QuadLeft + QuadRight) / 2f, (QuadTop + QuadBottom) / 2f);

    private const float LightResolution = 4;

    private readonly SDL_GPUDevice* _device;
    private readonly SDL_Window* _window;

    private readonly LightingGpuPipeline _gpuPipeline;
    private readonly SDL_GPUBuffer* _vertexBuffer;
    private readonly SDL_GPUTexture*[] _textures = new SDL_GPUTexture*[1];
    private readonly List<PointLight2D> _lights = new(3);
    private readonly List<SpotLight2D> _spotLights = new(2);

    private float _lighting;

    public LightingSample(SDL_GPUDevice* device, SDL_Window* window)
    {
        _device = device;
        _window = window;

        _gpuPipeline = new LightingGpuPipeline(device, window);
        _gpuPipeline.Parameters.Lights = _lights;
        _gpuPipeline.Parameters.SpotLights = _spotLights;

        var topLeftCorner = new Vector2(QuadLeft, QuadTop);
        var topRightCorner = new Vector2(QuadRight, QuadTop);
        const float outerAngle = MathF.PI / 4.5f;
        const float innerAngle = MathF.PI / 7f;
        const float spotRadius = 1500f;

        _spotLights.Add(new SpotLight2D(topLeftCorner, new Vector2(1f, 1f), outerAngle, innerAngle, spotRadius, 0xff80ff, intensity: 1.0f));
        _spotLights.Add(new SpotLight2D(topRightCorner, new Vector2(-1f, 1f), outerAngle, innerAngle, spotRadius, 0x80ffff, intensity: 1.0f));

        // top-left = white, bottom-left = yellow, bottom-right = cyan, top-right = magenta; premultiplied alpha 0.5 for all
        RgbaColor topLeft = RgbaColor.White;
        RgbaColor bottomLeft = RgbaColor.White;
        RgbaColor bottomRight = RgbaColor.White;
        RgbaColor topRight = RgbaColor.White;

        ReadOnlySpan<Vertex2D> vertices =
        [
            new(new Vector2(QuadLeft, QuadTop), new Vector2(0f, 0f), topLeft),
            new(new Vector2(QuadLeft, QuadBottom), new Vector2(0f, 1f), bottomLeft),
            new(new Vector2(QuadRight, QuadBottom), new Vector2(1f, 1f), bottomRight),

            new(new Vector2(QuadLeft, QuadTop), new Vector2(0f, 0f), topLeft),
            new(new Vector2(QuadRight, QuadBottom), new Vector2(1f, 1f), bottomRight),
            new(new Vector2(QuadRight, QuadTop), new Vector2(1f, 0f), topRight),
        ];

        uint vertexBufferSize = (uint)(vertices.Length * sizeof(Vertex2D));

        var vertexBufferCreateInfo = new SDL_GPUBufferCreateInfo
        {
            usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX,
            size = vertexBufferSize,
        };
        _vertexBuffer = SDL_CreateGPUBuffer(device, &vertexBufferCreateInfo);

        var transferBufferCreateInfo = new SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = vertexBufferSize,
        };
        SDL_GPUTransferBuffer* transferBuffer = SDL_CreateGPUTransferBuffer(device, &transferBufferCreateInfo);

        void* mapped = (void*)SDL_MapGPUTransferBuffer(device, transferBuffer, false);
        vertices.CopyTo(new Span<Vertex2D>(mapped, vertices.Length));
        SDL_UnmapGPUTransferBuffer(device, transferBuffer);

        byte[] jpgBytes = LoadEmbeddedBytes("Assets.Sample1.jpg");

        SDL_GPUCommandBuffer* uploadCommandBuffer = SDL_AcquireGPUCommandBuffer(device);
        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(uploadCommandBuffer);

        var transferBufferLocation = new SDL_GPUTransferBufferLocation
        {
            transfer_buffer = transferBuffer,
            offset = 0,
        };
        var bufferRegion = new SDL_GPUBufferRegion
        {
            buffer = _vertexBuffer,
            offset = 0,
            size = vertexBufferSize,
        };
        SDL_UploadToGPUBuffer(copyPass, &transferBufferLocation, &bufferRegion, false);

        fixed (byte* jpgPtr = jpgBytes)
        {
            SDL_IOStream* io = SDL_IOFromConstMem((IntPtr)jpgPtr, (nuint)jpgBytes.Length);
            int width, height;
            _textures[0] = IMG_LoadGPUTexture_IO(device, copyPass, io, true, &width, &height);
        }

        if (_textures[0] == null)
        {
            throw new InvalidOperationException($"IMG_LoadGPUTexture_IO failed: {SDL_GetError()}");
        }

        SDL_EndGPUCopyPass(copyPass);
        SDL_SubmitGPUCommandBuffer(uploadCommandBuffer);
        SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
    }

    public void Render()
    {
        SDL_GPUCommandBuffer* commandBuffer = SDL_AcquireGPUCommandBuffer(_device);

        SDL_GPUTexture* swapchainTexture;
        uint swapchainWidth, swapchainHeight;

        if (!SDL_WaitAndAcquireGPUSwapchainTexture(commandBuffer, _window, &swapchainTexture, &swapchainWidth, &swapchainHeight))
        {
            Console.Error.WriteLine($"SDL_WaitAndAcquireGPUSwapchainTexture failed: {SDL_GetError()}");
            SDL_SubmitGPUCommandBuffer(commandBuffer);
            return;
        }

        if (swapchainTexture != null)
        {
            FillAnimatedLights();

            if (Random.Shared.NextDouble() < 0.01)
            {
                _lighting = 1;
            }
            
            _gpuPipeline.ScreenSize = new Size((int)swapchainWidth, (int)swapchainHeight);
            _gpuPipeline.Parameters.Ambient = RgbaColor.White * _lighting;

            _lighting = MathF.Max(0.25f, _lighting - 1/15f);
            
            _gpuPipeline.Parameters.Resolution = LightResolution;
 
            var size = new Vector2(QuadRight - QuadLeft, QuadBottom - QuadTop);

            _gpuPipeline.Scale = 1.0f;
            _gpuPipeline.Offset = (_gpuPipeline.ScreenSize.ToVector() / _gpuPipeline.Scale - size) / 2;

            var colorTargetInfo = new SDL_GPUColorTargetInfo
            {
                texture = swapchainTexture,
                clear_color = new SDL_FColor { r = 0.1f, g = 0.1f, b = 0.15f, a = 1.0f },
                load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            };

            SDL_GPURenderPass* renderPass = SDL_BeginGPURenderPass(commandBuffer, &colorTargetInfo, 1, null);

            _gpuPipeline.Bind(commandBuffer, renderPass, _textures);

            var vertexBufferBinding = new SDL_GPUBufferBinding { buffer = _vertexBuffer, offset = 0 };
            SDL_BindGPUVertexBuffers(renderPass, 0, &vertexBufferBinding, 1);

            SDL_DrawGPUPrimitives(renderPass, 6, 1, 0, 0);
            SDL_EndGPURenderPass(renderPass);
        }

        SDL_SubmitGPUCommandBuffer(commandBuffer);
    }

    private void FillAnimatedLights()
    {
        float time = SDL_GetTicks() / 1000f;

        var orbitA = QuadCenter + new Vector2(MathF.Cos(time * 0.9f), MathF.Sin(time * 0.9f)) * (QuadRight - QuadLeft) / 2f;
        var orbitB = QuadCenter + new Vector2(MathF.Cos(time * -1.4f + 1.5f), MathF.Sin(time * -1.4f + 1.5f)) * 160f;
        var fixedPosition = new Vector2(QuadLeft - 40f, QuadBottom + 40f);

        _lights.Clear();
        _lights.Add(new PointLight2D(orbitA, radius: 500f, RgbaColor.White, intensity: 2.0f));
        _lights.Add(new PointLight2D(orbitB, radius: 200f, RgbaColor.Cyan, intensity: 1.0f));
        _lights.Add(new PointLight2D(fixedPosition, radius: 260f, RgbaColor.Magenta, intensity: 0.8f));
    }

    public void Dispose()
    {
        SDL_ReleaseGPUTexture(_device, _textures[0]);
        SDL_ReleaseGPUBuffer(_device, _vertexBuffer);
        _gpuPipeline.Dispose();
    }

    private static byte[] LoadEmbeddedBytes(string resourceName)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
