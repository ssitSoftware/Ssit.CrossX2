using System.Numerics;
using System.Runtime.InteropServices;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Effects;
using Ssit.CrossX2.Framework.Services;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Effects;

/// <summary>
/// Postprocessing effect: takes a regular texture and a glow texture, blurs the glow texture,
/// and composites both (regular opaque, glow additive) onto a caller-supplied output texture.
/// </summary>
internal sealed unsafe class GlowEffect : IGlowEffect
{
    private readonly SdlGpuRenderer _renderer;
    private readonly IActionScheduler _scheduler;
    private const float BlurSpreadPixels = 1f;
    private const int BlurIterations = 2;
    private const float BlurPadding = 240f;

    private readonly SDL_GPUDevice* _device;

    private readonly SDL_GPUGraphicsPipeline* _blurPipeline;
    private readonly SDL_GPUGraphicsPipeline* _blitPipeline;
    private readonly SDL_GPUGraphicsPipeline* _additivePipeline;
    private readonly SDL_GPUSampler* _sampler;

    private readonly SDL_GPUTexture* _blurTextureA;
    private readonly SDL_GPUTexture* _blurTextureB;

    private readonly SDL_GPUBuffer* _fullscreenVertexBuffer;
    private readonly SDL_GPUBuffer* _insetVertexBuffer;
    private readonly SDL_GPUBuffer* _glowCompositeVertexBuffer;

    private readonly float _spreadU;
    private readonly float _spreadV;
    private bool _disposed;

    public GlowEffect(SdlGpuRenderer renderer, IActionScheduler scheduler, Size glowSize)
    {
        _renderer = renderer;
        _scheduler = scheduler;
        SDL_GPUTextureFormat format = SDL_GetGPUSwapchainTextureFormat(renderer.Device, renderer.Window);
        
        _device = renderer.Device;

        _blurPipeline = CreateOffscreenPipeline(renderer, format);
        _blitPipeline = CreateCompositePipeline(renderer, format, enableBlend: false);
        _additivePipeline = CreateCompositePipeline(renderer, format, enableBlend: true);

        var samplerCreateInfo = new SDL_GPUSamplerCreateInfo
        {
            min_filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
            mag_filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
            mipmap_mode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,
            address_mode_u = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_v = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_w = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        };
        _sampler = SDL_CreateGPUSampler(_device, &samplerCreateInfo);

        if (_sampler == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUSampler failed: {SDL_GetError()}");
        }

        int canvasWidth = glowSize.Width + (int)(BlurPadding * 2);
        int canvasHeight = glowSize.Height + (int)(BlurPadding * 2);

        var blurTextureCreateInfo = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = format,
            usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET,
            width = (uint)canvasWidth,
            height = (uint)canvasHeight,
            layer_count_or_depth = 1,
            num_levels = 1,
            sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
        };
        _blurTextureA = SDL_CreateGPUTexture(_device, &blurTextureCreateInfo);
        _blurTextureB = SDL_CreateGPUTexture(_device, &blurTextureCreateInfo);

        if (_blurTextureA == null || _blurTextureB == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUTexture failed: {SDL_GetError()}");
        }

        _spreadU = BlurSpreadPixels / canvasWidth;
        _spreadV = BlurSpreadPixels / canvasHeight;

        // Full -1..1 NDC quad, full 0..1 uv - used for blur passes (which cover the entire
        // padded canvas) and for blitting the regular texture across the whole output target.
        ReadOnlySpan<Vertex2D> fullscreenVertices =
        [
            new(new Vector2(-1f, 1f), new Vector2(0f, 0f), RgbaColor.White),
            new(new Vector2(-1f, -1f), new Vector2(0f, 1f), RgbaColor.White),
            new(new Vector2(1f, -1f), new Vector2(1f, 1f), RgbaColor.White),

            new(new Vector2(-1f, 1f), new Vector2(0f, 0f), RgbaColor.White),
            new(new Vector2(1f, -1f), new Vector2(1f, 1f), RgbaColor.White),
            new(new Vector2(1f, 1f), new Vector2(1f, 0f), RgbaColor.White),
        ];

        // Inset rect (in the padded canvas's NDC space) where the source glow image is copied
        // before blurring, leaving transparent padding around it for the blur to expand into.
        float insetLeft = BlurPadding / canvasWidth * 2f - 1f;
        float insetRight = (BlurPadding + glowSize.Width) / canvasWidth * 2f - 1f;
        float insetTop = 1f - BlurPadding / canvasHeight * 2f;
        float insetBottom = 1f - (BlurPadding + glowSize.Height) / canvasHeight * 2f;

        ReadOnlySpan<Vertex2D> insetVertices =
        [
            new(new Vector2(insetLeft, insetTop), new Vector2(0f, 0f), RgbaColor.White),
            new(new Vector2(insetLeft, insetBottom), new Vector2(0f, 1f), RgbaColor.White),
            new(new Vector2(insetRight, insetBottom), new Vector2(1f, 1f), RgbaColor.White),

            new(new Vector2(insetLeft, insetTop), new Vector2(0f, 0f), RgbaColor.White),
            new(new Vector2(insetRight, insetBottom), new Vector2(1f, 1f), RgbaColor.White),
            new(new Vector2(insetRight, insetTop), new Vector2(1f, 0f), RgbaColor.White),
        ];

        // Full -1..1 NDC quad for the output target, but sampling only the sub-rect of the
        // blurred canvas that corresponds to the original glow image - crops the blur bleed
        // back to the output texture's bounds instead of letting it overflow.
        var compositeUvLeft = BlurPadding / canvasWidth;
        var compositeUvRight = (BlurPadding + glowSize.Width) / canvasWidth;
        var compositeUvTop = BlurPadding / canvasHeight;
        var compositeUvBottom = (BlurPadding + glowSize.Height) / canvasHeight;

        ReadOnlySpan<Vertex2D> glowCompositeVertices =
        [
            new(new Vector2(-1f, 1f), new Vector2(compositeUvLeft, compositeUvTop), RgbaColor.White),
            new(new Vector2(-1f, -1f), new Vector2(compositeUvLeft, compositeUvBottom), RgbaColor.White),
            new(new Vector2(1f, -1f), new Vector2(compositeUvRight, compositeUvBottom), RgbaColor.White),

            new(new Vector2(-1f, 1f), new Vector2(compositeUvLeft, compositeUvTop), RgbaColor.White),
            new(new Vector2(1f, -1f), new Vector2(compositeUvRight, compositeUvBottom), RgbaColor.White),
            new(new Vector2(1f, 1f), new Vector2(compositeUvRight, compositeUvTop), RgbaColor.White),
        ];

        SDL_GPUCommandBuffer* uploadCommandBuffer = _renderer.CommandBuffer;
        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(uploadCommandBuffer);

        _fullscreenVertexBuffer = CreateAndUploadVertexBuffer(_device, copyPass, fullscreenVertices);
        _insetVertexBuffer = CreateAndUploadVertexBuffer(_device, copyPass, insetVertices);
        _glowCompositeVertexBuffer = CreateAndUploadVertexBuffer(_device, copyPass, glowCompositeVertices);

        SDL_EndGPUCopyPass(copyPass);
        _renderer.SubmitCommandBuffer();
    }

    public void Render(SDL_GPUCommandBuffer* commandBuffer, SDL_GPUTexture* regularTexture, SDL_GPUTexture* glowTexture, SDL_GPUTexture* outputTexture, float intensity, float sourceScale = 1)
    {
        RunBlurPass(commandBuffer, glowTexture, _blurTextureA, Vector2.Zero, _insetVertexBuffer);

        for (int i = 0; i < BlurIterations; i++)
        {
            float scale = sourceScale;
            RunBlurPass(commandBuffer, _blurTextureA, _blurTextureB, new Vector2(_spreadU * scale, 0f), _fullscreenVertexBuffer);
            RunBlurPass(commandBuffer, _blurTextureB, _blurTextureA, new Vector2(0f, _spreadV * scale), _fullscreenVertexBuffer);
        }

        var colorTargetInfo = new SDL_GPUColorTargetInfo
        {
            texture = outputTexture,
            clear_color = new SDL_FColor { r = 0f, g = 0f, b = 0f, a = 0f },
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };
        SDL_GPURenderPass* renderPass = SDL_BeginGPURenderPass(commandBuffer, &colorTargetInfo, 1, null);

        SDL_BindGPUGraphicsPipeline(renderPass, _blitPipeline);

        var blitUniforms = new GlowUniforms { Intensity = new Vector4(1f, 0f, 0f, 0f) };
        SDL_PushGPUFragmentUniformData(commandBuffer, 0, (IntPtr)(&blitUniforms), (uint)sizeof(GlowUniforms));

        var regularBinding = new SDL_GPUTextureSamplerBinding { texture = regularTexture, sampler = _sampler };
        SDL_BindGPUFragmentSamplers(renderPass, 0, &regularBinding, 1);

        var fullscreenBinding = new SDL_GPUBufferBinding { buffer = _fullscreenVertexBuffer, offset = 0 };
        SDL_BindGPUVertexBuffers(renderPass, 0, &fullscreenBinding, 1);
        SDL_DrawGPUPrimitives(renderPass, 6, 1, 0, 0);

        SDL_BindGPUGraphicsPipeline(renderPass, _additivePipeline);

        var glowUniforms = new GlowUniforms { Intensity = new Vector4(intensity, 0f, 0f, 0f) };
        SDL_PushGPUFragmentUniformData(commandBuffer, 0, (IntPtr)(&glowUniforms), (uint)sizeof(GlowUniforms));

        var glowBinding = new SDL_GPUTextureSamplerBinding { texture = _blurTextureA, sampler = _sampler };
        SDL_BindGPUFragmentSamplers(renderPass, 0, &glowBinding, 1);

        var glowCompositeBinding = new SDL_GPUBufferBinding { buffer = _glowCompositeVertexBuffer, offset = 0 };
        SDL_BindGPUVertexBuffers(renderPass, 0, &glowCompositeBinding, 1);
        SDL_DrawGPUPrimitives(renderPass, 6, 1, 0, 0);

        SDL_EndGPURenderPass(renderPass);
    }

    private void RunBlurPass(SDL_GPUCommandBuffer* commandBuffer, SDL_GPUTexture* source, SDL_GPUTexture* destination, Vector2 direction, SDL_GPUBuffer* vertexBuffer)
    {
        var colorTargetInfo = new SDL_GPUColorTargetInfo
        {
            texture = destination,
            clear_color = new SDL_FColor { r = 0f, g = 0f, b = 0f, a = 0f },
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };

        SDL_GPURenderPass* renderPass = SDL_BeginGPURenderPass(commandBuffer, &colorTargetInfo, 1, null);
        SDL_BindGPUGraphicsPipeline(renderPass, _blurPipeline);

        var blurUniforms = new BlurUniforms { Direction = new Vector4(direction.X, direction.Y, 0f, 0f) };
        SDL_PushGPUFragmentUniformData(commandBuffer, 0, (IntPtr)(&blurUniforms), (uint)sizeof(BlurUniforms));

        var samplerBinding = new SDL_GPUTextureSamplerBinding { texture = source, sampler = _sampler };
        SDL_BindGPUFragmentSamplers(renderPass, 0, &samplerBinding, 1);

        var vertexBufferBinding = new SDL_GPUBufferBinding { buffer = vertexBuffer, offset = 0 };
        SDL_BindGPUVertexBuffers(renderPass, 0, &vertexBufferBinding, 1);

        SDL_DrawGPUPrimitives(renderPass, 6, 1, 0, 0);
        SDL_EndGPURenderPass(renderPass);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _scheduler.Schedule(() =>
        {
            SDL_ReleaseGPUTexture(_device, _blurTextureA);
            SDL_ReleaseGPUTexture(_device, _blurTextureB);
            SDL_ReleaseGPUBuffer(_device, _fullscreenVertexBuffer);
            SDL_ReleaseGPUBuffer(_device, _insetVertexBuffer);
            SDL_ReleaseGPUBuffer(_device, _glowCompositeVertexBuffer);
            SDL_ReleaseGPUSampler(_device, _sampler);
            SDL_ReleaseGPUGraphicsPipeline(_device, _blurPipeline);
            SDL_ReleaseGPUGraphicsPipeline(_device, _blitPipeline);
            SDL_ReleaseGPUGraphicsPipeline(_device, _additivePipeline);
        });
    }

    public void Render(IRenderTarget target, ITexture normal, ITexture glow, float sourceScale = 1)
    {
        _renderer.SubmitCommandBuffer();

        var sdlRenderTarget = (SdlGpuRenderTarget)target;
        var sdlNormalTexture = (ISdlGpuTexture)normal;
        var sdlGlowTexture = (ISdlGpuTexture)glow;

        Render(_renderer.CommandBuffer, sdlNormalTexture.GetMap(TextureMaps.Diffuse), sdlGlowTexture.GetMap(TextureMaps.Diffuse), sdlRenderTarget.GetMap(TextureMaps.Diffuse), 1f, sourceScale);

        _renderer.SubmitCommandBuffer();
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

    private static SDL_GPUGraphicsPipeline* CreateOffscreenPipeline(SdlGpuRenderer renderer, SDL_GPUTextureFormat targetFormat)
    {
        SDL_GPUShader* vertexShader = GpuShader.CreateFromEmbeddedResource(renderer, "Effects.FullscreenQuad.vert", "vertexMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX);
        SDL_GPUShader* fragmentShader = GpuShader.CreateFromEmbeddedResource(renderer, "Effects.Blur.frag", "fragmentMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT, numSamplers: 1, numUniformBuffers: 1);

        if (vertexShader == null || fragmentShader == null)
        {
            throw new InvalidOperationException($"Shader creation failed: {SDL_GetError()}");
        }

        SDL_GPUGraphicsPipeline* pipeline = CreatePipeline(renderer.Device, vertexShader, fragmentShader, targetFormat, enableBlend: false);

        SDL_ReleaseGPUShader(renderer.Device, vertexShader);
        SDL_ReleaseGPUShader(renderer.Device, fragmentShader);

        return pipeline;
    }

    private static SDL_GPUGraphicsPipeline* CreateCompositePipeline(SdlGpuRenderer renderer, SDL_GPUTextureFormat targetFormat, bool enableBlend)
    {
        SDL_GPUShader* vertexShader = GpuShader.CreateFromEmbeddedResource(renderer, "Effects.FullscreenQuad.vert", "vertexMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX);
        SDL_GPUShader* fragmentShader = GpuShader.CreateFromEmbeddedResource(renderer, "Effects.GlowComposite.frag", "fragmentMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT, numSamplers: 1, numUniformBuffers: 1);

        if (vertexShader == null || fragmentShader == null)
        {
            throw new InvalidOperationException($"Shader creation failed: {SDL_GetError()}");
        }

        SDL_GPUGraphicsPipeline* pipeline = CreatePipeline(renderer.Device, vertexShader, fragmentShader, targetFormat, enableBlend);

        SDL_ReleaseGPUShader(renderer.Device, vertexShader);
        SDL_ReleaseGPUShader(renderer.Device, fragmentShader);

        return pipeline;
    }

    private static SDL_GPUGraphicsPipeline* CreatePipeline(SDL_GPUDevice* device, SDL_GPUShader* vertexShader, SDL_GPUShader* fragmentShader, SDL_GPUTextureFormat targetFormat, bool enableBlend)
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

        var blendState = enableBlend
            ? new SDL_GPUColorTargetBlendState
            {
                enable_blend = true,
                src_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                dst_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                src_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                dst_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
            }
            : new SDL_GPUColorTargetBlendState { enable_blend = false };

        var colorTargetDescriptions = stackalloc SDL_GPUColorTargetDescription[1];
        colorTargetDescriptions[0] = new SDL_GPUColorTargetDescription
        {
            format = targetFormat,
            blend_state = blendState,
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
    private struct BlurUniforms
    {
        public Vector4 Direction;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GlowUniforms
    {
        public Vector4 Intensity;
    }
}
