using System.Numerics;
using System.Runtime.InteropServices;
using CrossX2;
using SDL;
using static SDL.SDL3;

namespace Sdl3Hw;

/// <summary>
/// Postprocessing effect: takes a single source texture and renders it onto an output texture,
/// scaled uniformly to fill the output while emulating a CRT display (barrel distortion,
/// RGB channel displacement, scanlines, vignette).
/// </summary>
public sealed unsafe class CrtSimEffect : IDisposable
{
    private readonly SDL_GPUDevice* _device;

    private readonly SDL_GPUGraphicsPipeline* _pipeline;
    private readonly SDL_GPUSampler* _sampler;
    private readonly SDL_GPUBuffer* _vertexBuffer;

    public float BarrelDistortion { get; set; } = 0.02f;
    public float RgbDisplacement { get; set; } = 0.25f;
    public float ScanlineIntensity { get; set; } = 0.2f;
    public float Vignette { get; set; } = 0.25f;

    public CrtSimEffect(SDL_GPUDevice* device, SDL_GPUTextureFormat targetFormat)
    {
        _device = device;

        SDL_GPUShader* vertexShader = GpuShader.CreateFromEmbeddedResource(device, "Shaders.Crt.vert.metal", "vertexMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX, numUniformBuffers: 1);
        SDL_GPUShader* fragmentShader = GpuShader.CreateFromEmbeddedResource(device, "Shaders.Crt.frag.metal", "fragmentMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT, numSamplers: 1, numUniformBuffers: 1);

        if (vertexShader == null || fragmentShader == null)
        {
            throw new InvalidOperationException($"Shader creation failed: {SDL_GetError()}");
        }

        _pipeline = CreatePipeline(device, vertexShader, fragmentShader, targetFormat);

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

        ReadOnlySpan<Vertex2D> quadVertices =
        [
            new(new Vector2(-1f, 1f), new Vector2(0f, 0f), RgbaColor.White),
            new(new Vector2(-1f, -1f), new Vector2(0f, 1f), RgbaColor.White),
            new(new Vector2(1f, -1f), new Vector2(1f, 1f), RgbaColor.White),

            new(new Vector2(-1f, 1f), new Vector2(0f, 0f), RgbaColor.White),
            new(new Vector2(1f, -1f), new Vector2(1f, 1f), RgbaColor.White),
            new(new Vector2(1f, 1f), new Vector2(1f, 0f), RgbaColor.White),
        ];

        SDL_GPUCommandBuffer* uploadCommandBuffer = SDL_AcquireGPUCommandBuffer(device);
        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(uploadCommandBuffer);
        _vertexBuffer = CreateAndUploadVertexBuffer(device, copyPass, quadVertices);
        SDL_EndGPUCopyPass(copyPass);
        SDL_SubmitGPUCommandBuffer(uploadCommandBuffer);
    }

    public void Render(SDL_GPUCommandBuffer* commandBuffer, SDL_GPUTexture* sourceTexture, int sourceWidth, int sourceHeight,
        SDL_GPUTexture* outputTexture, uint outputWidth, uint outputHeight, SDL_FColor clearColor)
    {
        // Uniform scale (same factor on both axes) so the source fills as much of the output
        // as possible without cropping or stretching - i.e. an aspect-correct contain fit.
        float fitScale = Math.Min((float)outputWidth / sourceWidth, (float)outputHeight / sourceHeight);
        float scaleX = sourceWidth * fitScale / outputWidth;
        float scaleY = sourceHeight * fitScale / outputHeight;

        var colorTargetInfo = new SDL_GPUColorTargetInfo
        {
            texture = outputTexture,
            clear_color = clearColor,
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };

        SDL_GPURenderPass* renderPass = SDL_BeginGPURenderPass(commandBuffer, &colorTargetInfo, 1, null);
        SDL_BindGPUGraphicsPipeline(renderPass, _pipeline);

        var scaleUniforms = new ScaleUniforms { Scale = new Vector4(scaleX, scaleY, 0f, 0f) };
        SDL_PushGPUVertexUniformData(commandBuffer, 0, (IntPtr)(&scaleUniforms), (uint)sizeof(ScaleUniforms));

        var crtUniforms = new CrtUniforms
        {
            Distortion = new Vector4(BarrelDistortion, RgbDisplacement, ScanlineIntensity, Vignette),
            Params = new Vector4(fitScale, 0f, 0f, 0f),
        };
        SDL_PushGPUFragmentUniformData(commandBuffer, 0, (IntPtr)(&crtUniforms), (uint)sizeof(CrtUniforms));

        var samplerBinding = new SDL_GPUTextureSamplerBinding { texture = sourceTexture, sampler = _sampler };
        SDL_BindGPUFragmentSamplers(renderPass, 0, &samplerBinding, 1);

        var vertexBufferBinding = new SDL_GPUBufferBinding { buffer = _vertexBuffer, offset = 0 };
        SDL_BindGPUVertexBuffers(renderPass, 0, &vertexBufferBinding, 1);

        SDL_DrawGPUPrimitives(renderPass, 6, 1, 0, 0);

        SDL_EndGPURenderPass(renderPass);
    }

    public void Dispose()
    {
        SDL_ReleaseGPUBuffer(_device, _vertexBuffer);
        SDL_ReleaseGPUSampler(_device, _sampler);
        SDL_ReleaseGPUGraphicsPipeline(_device, _pipeline);
    }

    private static SDL_GPUBuffer* CreateAndUploadVertexBuffer(SDL_GPUDevice* device, SDL_GPUCopyPass* copyPass, ReadOnlySpan<Vertex2D> vertices)
    {
        uint vertexBufferSize = (uint)(vertices.Length * sizeof(Vertex2D));

        var vertexBufferCreateInfo = new SDL_GPUBufferCreateInfo
        {
            usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX,
            size = vertexBufferSize,
        };
        SDL_GPUBuffer* vertexBuffer = SDL_CreateGPUBuffer(device, &vertexBufferCreateInfo);

        var transferBufferCreateInfo = new SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = vertexBufferSize,
        };
        SDL_GPUTransferBuffer* transferBuffer = SDL_CreateGPUTransferBuffer(device, &transferBufferCreateInfo);

        void* mapped = (void*)SDL_MapGPUTransferBuffer(device, transferBuffer, false);
        vertices.CopyTo(new Span<Vertex2D>(mapped, vertices.Length));
        SDL_UnmapGPUTransferBuffer(device, transferBuffer);

        var transferBufferLocation = new SDL_GPUTransferBufferLocation { transfer_buffer = transferBuffer, offset = 0 };
        var bufferRegion = new SDL_GPUBufferRegion { buffer = vertexBuffer, offset = 0, size = vertexBufferSize };
        SDL_UploadToGPUBuffer(copyPass, &transferBufferLocation, &bufferRegion, false);

        SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

        return vertexBuffer;
    }

    private static SDL_GPUGraphicsPipeline* CreatePipeline(SDL_GPUDevice* device, SDL_GPUShader* vertexShader, SDL_GPUShader* fragmentShader, SDL_GPUTextureFormat targetFormat)
    {
        var vertexBufferDescriptions = stackalloc SDL_GPUVertexBufferDescription[1];
        vertexBufferDescriptions[0] = new SDL_GPUVertexBufferDescription
        {
            slot = 0,
            pitch = (uint)sizeof(Vertex2D),
            input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
            instance_step_rate = 0,
        };

        var vertexAttributes = stackalloc SDL_GPUVertexAttribute[3];
        vertexAttributes[0] = new SDL_GPUVertexAttribute
        {
            location = 0,
            buffer_slot = 0,
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            offset = 0,
        };
        vertexAttributes[1] = new SDL_GPUVertexAttribute
        {
            location = 1,
            buffer_slot = 0,
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            offset = (uint)sizeof(Vector2),
        };
        vertexAttributes[2] = new SDL_GPUVertexAttribute
        {
            location = 2,
            buffer_slot = 0,
            format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
            offset = (uint)(sizeof(Vector2) * 2),
        };

        var colorTargetDescriptions = stackalloc SDL_GPUColorTargetDescription[1];
        colorTargetDescriptions[0] = new SDL_GPUColorTargetDescription
        {
            format = targetFormat,
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

        SDL_GPUGraphicsPipeline* pipeline = SDL_CreateGPUGraphicsPipeline(device, &pipelineCreateInfo);

        if (pipeline == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUGraphicsPipeline failed: {SDL_GetError()}");
        }

        return pipeline;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ScaleUniforms
    {
        public Vector4 Scale;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CrtUniforms
    {
        public Vector4 Distortion;
        public Vector4 Params;
    }
}
