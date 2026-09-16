using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using CrossX2;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_image;

namespace Sdl3Hw;

public sealed unsafe class TextureSample : ISample
{
    private readonly SDL_GPUDevice* _device;
    private readonly SDL_Window* _window;

    private readonly SDL_GPUGraphicsPipeline* _pipeline;
    private readonly SDL_GPUBuffer* _vertexBuffer;
    private readonly SDL_GPUSampler* _sampler;
    private readonly SDL_GPUTexture* _texture;

    public TextureSample(SDL_GPUDevice* device, SDL_Window* window)
    {
        _device = device;
        _window = window;

        SDL_GPUShader* vertexShader = GpuShader.CreateFromEmbeddedResource(device, "Shaders.Texture.vert.metal", "vertexMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX);
        SDL_GPUShader* fragmentShader = GpuShader.CreateFromEmbeddedResource(device, "Shaders.Texture.frag.metal", "fragmentMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT, numSamplers: 1);

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

        var vertexAttributes = stackalloc SDL_GPUVertexAttribute[3];
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
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            offset = (uint)sizeof(Vector3),
        };
        vertexAttributes[2] = new SDL_GPUVertexAttribute
        {
            location = 2,
            buffer_slot = 0,
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
            offset = (uint)(sizeof(Vector3) + sizeof(Vector2)),
        };

        var colorTargetDescriptions = stackalloc SDL_GPUColorTargetDescription[1];
        colorTargetDescriptions[0] = new SDL_GPUColorTargetDescription
        {
            format = SDL_GetGPUSwapchainTextureFormat(device, window),
            blend_state = new SDL_GPUColorTargetBlendState
            {
                enable_blend = true,
                src_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                dst_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                src_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                dst_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
            },
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
                num_vertex_attributes = 3,
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

        var samplerCreateInfo = new SDL_GPUSamplerCreateInfo
        {
            min_filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
            mag_filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
            mipmap_mode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,
            address_mode_u = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_v = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_w = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        };
        _sampler = SDL_CreateGPUSampler(device, &samplerCreateInfo);

        if (_sampler == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUSampler failed: {SDL_GetError()}");
        }

        // top-left = white, bottom-left = yellow, bottom-right = cyan, top-right = magenta; premultiplied alpha 0.5 for all
        RgbaColor topLeft = RgbaColor.White * 0.5f;
        RgbaColor bottomLeft = RgbaColor.Yellow * 0.5f;
        RgbaColor bottomRight = RgbaColor.Cyan * 0.5f;
        RgbaColor topRight = RgbaColor.Magenta * 0.5f;

        ReadOnlySpan<Vertex> vertices =
        [
            new Vertex(new Vector3(-0.8f, 0.8f, 0f), new Vector2(0f, 0f), topLeft),
            new Vertex(new Vector3(-0.8f, -0.8f, 0f), new Vector2(0f, 1f), bottomLeft),
            new Vertex(new Vector3(0.8f, -0.8f, 0f), new Vector2(1f, 1f), bottomRight),

            new Vertex(new Vector3(-0.8f, 0.8f, 0f), new Vector2(0f, 0f), topLeft),
            new Vertex(new Vector3(0.8f, -0.8f, 0f), new Vector2(1f, 1f), bottomRight),
            new Vertex(new Vector3(0.8f, 0.8f, 0f), new Vector2(1f, 0f), topRight),
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
            _texture = IMG_LoadGPUTexture_IO(device, copyPass, io, true, &width, &height);
        }

        if (_texture == null)
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

            var samplerBinding = new SDL_GPUTextureSamplerBinding { texture = _texture, sampler = _sampler };
            SDL_BindGPUFragmentSamplers(renderPass, 0, &samplerBinding, 1);

            SDL_DrawGPUPrimitives(renderPass, 6, 1, 0, 0);

            SDL_EndGPURenderPass(renderPass);
        }

        SDL_SubmitGPUCommandBuffer(commandBuffer);
    }

    public void Dispose()
    {
        SDL_ReleaseGPUTexture(_device, _texture);
        SDL_ReleaseGPUSampler(_device, _sampler);
        SDL_ReleaseGPUBuffer(_device, _vertexBuffer);
        SDL_ReleaseGPUGraphicsPipeline(_device, _pipeline);
    }

    private static byte[] LoadEmbeddedBytes(string resourceName)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex(Vector3 position, Vector2 texCoordinates, RgbaColor color)
    {
        public Vector3 Position = position;
        public Vector2 TexCoordinates = texCoordinates;
        public RgbaColor Color = color;
    }
}
