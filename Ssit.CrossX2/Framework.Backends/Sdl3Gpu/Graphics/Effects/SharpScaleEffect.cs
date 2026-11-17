using System.Numerics;
using System.Runtime.InteropServices;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Effects;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Effects;

/// <summary>
/// Draws a texture into a rectangle of the renderer's current output target in a single pass, using
/// "sharp bilinear" filtering (see SharpScale.frag). Replaces the older approach of point-upscaling into an
/// intermediate target at an integer multiple of the scene size and scaling that down linearly, which costs
/// several times more fill rate and bandwidth.
/// </summary>
internal sealed unsafe class SharpScaleEffect : ISharpScaleEffect
{
    private readonly SdlGpuRenderer _renderer;
    private readonly SharpScaleEffectParameters _parameters;
    private readonly SDL_GPUDevice* _device;

    private readonly SDL_GPUGraphicsPipeline* _pipeline;
    private readonly SDL_GPUSampler* _sampler;
    private readonly SDL_GPUBuffer* _vertexBuffer;
    private bool _disposed;

    public SharpScaleEffect(SdlGpuRenderer renderer, SharpScaleEffectParameters parameters = null)
    {
        _renderer = renderer;
        _parameters = parameters ?? new SharpScaleEffectParameters();
        _device = renderer.Device;

        SDL_GPUTextureFormat targetFormat = SDL_GetGPUSwapchainTextureFormat(_device, renderer.Window);

        SDL_GPUShader* vertexShader = GpuShader.CreateFromEmbeddedResource(renderer, "Effects.FullscreenQuad.vert", "vertexMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX);
        SDL_GPUShader* fragmentShader = GpuShader.CreateFromEmbeddedResource(renderer, "Effects.SharpScale.frag", "fragmentMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT, numSamplers: 1, numUniformBuffers: 1);

        if (vertexShader == null || fragmentShader == null)
        {
            throw new InvalidOperationException($"Shader creation failed: {SDL_GetError()}");
        }

        _pipeline = CreatePipeline(_device, vertexShader, fragmentShader, targetFormat);

        SDL_ReleaseGPUShader(_device, vertexShader);
        SDL_ReleaseGPUShader(_device, fragmentShader);

        var samplerCreateInfo = new SDL_GPUSamplerCreateInfo
        {
            min_filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
            mag_filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
            mipmap_mode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,
            address_mode_u = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_v = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_w = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        };
        _sampler = SDL_CreateGPUSampler(_device, &samplerCreateInfo);

        if (_sampler == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUSampler failed: {SDL_GetError()}");
        }

        // Full -1..1 NDC quad; the viewport places it at the destination rectangle.
        ReadOnlySpan<Vertex2D> quadVertices =
        [
            new(new Vector2(-1f, 1f), new Vector2(0f, 0f), RgbaColor.White),
            new(new Vector2(-1f, -1f), new Vector2(0f, 1f), RgbaColor.White),
            new(new Vector2(1f, -1f), new Vector2(1f, 1f), RgbaColor.White),

            new(new Vector2(-1f, 1f), new Vector2(0f, 0f), RgbaColor.White),
            new(new Vector2(1f, -1f), new Vector2(1f, 1f), RgbaColor.White),
            new(new Vector2(1f, 1f), new Vector2(1f, 0f), RgbaColor.White),
        ];

        // Own one-time command buffer, so the upload never lands in the middle of the shared per-frame one.
        SDL_GPUCommandBuffer* uploadCommandBuffer = SDL_AcquireGPUCommandBuffer(_device);

        if (uploadCommandBuffer == null)
            throw new InvalidOperationException($"SDL_AcquireGPUCommandBuffer failed: {SDL_GetError()}");

        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(uploadCommandBuffer);
        _vertexBuffer = CreateAndUploadVertexBuffer(_device, copyPass, quadVertices);
        SDL_EndGPUCopyPass(copyPass);
        SDL_SubmitGPUCommandBuffer(uploadCommandBuffer);
    }

    public void Render(ITexture source, RectangleF destination)
    {
        if (destination.Width <= 0 || destination.Height <= 0)
            return;

        var sdlSource = (ISdlGpuTexture)source;

        // Flush queued draws and close the renderer's pass, so this pass sees everything drawn so far.
        _renderer.EndCurrentGpuRenderPass();

        var commandBuffer = _renderer.CommandBuffer;

        var colorTargetInfo = new SDL_GPUColorTargetInfo
        {
            texture = _renderer.CurrentOutputTarget.Handle,
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };

        SDL_GPURenderPass* renderPass = SDL_BeginGPURenderPass(commandBuffer, &colorTargetInfo, 1, null);

        if (renderPass == null)
            throw new InvalidOperationException($"SDL_BeginGPURenderPass failed: {SDL_GetError()}");

        SDL_BindGPUGraphicsPipeline(renderPass, _pipeline);

        var viewport = new SDL_GPUViewport
        {
            x = destination.X,
            y = destination.Y,
            w = destination.Width,
            h = destination.Height,
            min_depth = 0f,
            max_depth = 1f,
        };
        SDL_SetGPUViewport(renderPass, &viewport);

        var sourceSize = source.Size;
        var uniforms = new SharpScaleUniforms
        {
            TextureSize = new Vector4(sourceSize.Width, sourceSize.Height, 1f / sourceSize.Width, 1f / sourceSize.Height),
            Scale = new Vector4(destination.Width / sourceSize.Width, destination.Height / sourceSize.Height, 0f, 0f),
            ColorGrading = new Vector4(_parameters.Gamma, _parameters.LightnessMultiplier, _parameters.Saturation,
                _parameters.IsIdentity ? 0f : 1f),
        };
        SDL_PushGPUFragmentUniformData(commandBuffer, 0, (IntPtr)(&uniforms), (uint)sizeof(SharpScaleUniforms));

        var samplerBinding = new SDL_GPUTextureSamplerBinding { texture = sdlSource.GetMap(TextureMaps.Diffuse), sampler = _sampler };
        SDL_BindGPUFragmentSamplers(renderPass, 0, &samplerBinding, 1);

        var vertexBufferBinding = new SDL_GPUBufferBinding { buffer = _vertexBuffer, offset = 0 };
        SDL_BindGPUVertexBuffers(renderPass, 0, &vertexBufferBinding, 1);

        SDL_DrawGPUPrimitives(renderPass, 6, 1, 0, 0);

        SDL_EndGPURenderPass(renderPass);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

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

        // Premultiplied alpha over what's already in the target, matching the sprite draw this replaces.
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
    private struct SharpScaleUniforms
    {
        public Vector4 TextureSize;
        public Vector4 Scale;
        public Vector4 ColorGrading;
    }
}
