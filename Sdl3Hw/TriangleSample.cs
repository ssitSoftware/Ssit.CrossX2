using System.Runtime.InteropServices;
using SDL;
using static SDL.SDL3;

namespace Sdl3Hw;

public sealed unsafe class TriangleSample : ISample
{
    private readonly SDL_GPUDevice* _device;
    private readonly SDL_Window* _window;

    private readonly SDL_GPUGraphicsPipeline* _pipeline;
    private readonly SDL_GPUBuffer* _vertexBuffer;

    public TriangleSample(SDL_GPUDevice* device, SDL_Window* window)
    {
        _device = device;
        _window = window;
        
        SDL_GPUShader* vertexShader = GpuShader.CreateFromEmbeddedResource(device, "Shaders.Triangle.vert.metal", "vertexMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX);
        SDL_GPUShader* fragmentShader = GpuShader.CreateFromEmbeddedResource(device, "Shaders.Triangle.frag.metal", "fragmentMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT);

        if (vertexShader == null || fragmentShader == null)
        {
            throw new InvalidOperationException($"Shader creation failed: {SDL_GetError()}");
        }

        var vertexBufferDescriptions = stackalloc SDL_GPUVertexBufferDescription[1];
        vertexBufferDescriptions[0] = new SDL_GPUVertexBufferDescription
        {
            slot = 0,
            pitch = (uint)sizeof(Vertex),
            input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
            instance_step_rate = 0,
        };

        var vertexAttributes = stackalloc SDL_GPUVertexAttribute[2];
        vertexAttributes[0] = new SDL_GPUVertexAttribute
        {
            location = 0,
            buffer_slot = 0,
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
            offset = 0,
        };
        vertexAttributes[1] = new SDL_GPUVertexAttribute
        {
            location = 1,
            buffer_slot = 0,
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
            offset = (uint)sizeof(float) * 3,
        };

        var colorTargetDescriptions = stackalloc SDL_GPUColorTargetDescription[1];
        colorTargetDescriptions[0] = new SDL_GPUColorTargetDescription
        {
            format = SDL_GetGPUSwapchainTextureFormat(device, window),
        };

        var pipelineCreateInfo = new SDL_GPUGraphicsPipelineCreateInfo
        {
            vertex_shader = vertexShader,
            fragment_shader = fragmentShader,
            primitive_type = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
            vertex_input_state = new SDL_GPUVertexInputState
            {
                vertex_buffer_descriptions = vertexBufferDescriptions,
                num_vertex_buffers = 1,
                vertex_attributes = vertexAttributes,
                num_vertex_attributes = 2,
            },
            target_info = new SDL_GPUGraphicsPipelineTargetInfo
            {
                color_target_descriptions = colorTargetDescriptions,
                num_color_targets = 1,
            },
        };

        _pipeline = SDL_CreateGPUGraphicsPipeline(device, &pipelineCreateInfo);

        if (_pipeline == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUGraphicsPipeline failed: {SDL_GetError()}");
        }

        SDL_ReleaseGPUShader(device, vertexShader);
        SDL_ReleaseGPUShader(device, fragmentShader);

        ReadOnlySpan<Vertex> vertices =
        [
            new Vertex(0.0f, 0.5f, 0.0f, 1f, 0f, 0f),
            new Vertex(-0.5f, -0.5f, 0.0f, 0f, 1f, 0f),
            new Vertex(0.5f, -0.5f, 0.0f, 0f, 0f, 1f),
        ];

        uint vertexBufferSize = (uint)(vertices.Length * sizeof(Vertex));

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
        vertices.CopyTo(new Span<Vertex>(mapped, vertices.Length));
        SDL_UnmapGPUTransferBuffer(device, transferBuffer);

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
            var colorTargetInfo = new SDL_GPUColorTargetInfo
            {
                texture = swapchainTexture,
                clear_color = new SDL_FColor { r = 0.1f, g = 0.1f, b = 0.15f, a = 1.0f },
                load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            };

            SDL_GPURenderPass* renderPass = SDL_BeginGPURenderPass(commandBuffer, &colorTargetInfo, 1, null);

            SDL_BindGPUGraphicsPipeline(renderPass, _pipeline);

            var vertexBufferBinding = new SDL_GPUBufferBinding { buffer = _vertexBuffer, offset = 0 };
            SDL_BindGPUVertexBuffers(renderPass, 0, &vertexBufferBinding, 1);

            SDL_DrawGPUPrimitives(renderPass, 3, 1, 0, 0);

            SDL_EndGPURenderPass(renderPass);
        }

        SDL_SubmitGPUCommandBuffer(commandBuffer);
    }

    public void Dispose()
    {
        SDL_ReleaseGPUBuffer(_device, _vertexBuffer);
        SDL_ReleaseGPUGraphicsPipeline(_device, _pipeline);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex(float x, float y, float z, float r, float g, float b)
    {
        public float X = x, Y = y, Z = z;
        public float R = r, G = g, B = b;
    }
}
