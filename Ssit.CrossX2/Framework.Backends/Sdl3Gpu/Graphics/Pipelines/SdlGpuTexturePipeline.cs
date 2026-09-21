using System.Numerics;
using System.Runtime.InteropServices;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Pipelines;

internal unsafe class SdlGpuTexturePipeline : ISdlGpuPipeline
{
    protected readonly SdlGpuRenderer GpuRenderer;
    
    private readonly SDL_GPUDevice* _device;
    public SDL_GPUGraphicsPipeline* Pipeline { get; }
    public SDL_GPUSampler* LinearSampler { get; }
    public SDL_GPUSampler* PointSampler { get; }

    public SdlGpuTexturePipeline(SdlGpuRenderer gpuRenderer)
        : this(gpuRenderer,
            "Pipelines.Texture.vert", 
            "Pipelines.Texture.frag", 
            fragmentUniformBuffers: 0)
    {
    }

    protected SdlGpuTexturePipeline(SdlGpuRenderer gpuRenderer, string vertexShaderResource, string fragmentShaderResource, int fragmentUniformBuffers, int fragmentSamplers = 1)
    {
        GpuRenderer = gpuRenderer;

        _device = gpuRenderer.Device;
        var window = gpuRenderer.Window;

        SDL_GPUShader* vertexShader = GpuShader.CreateFromEmbeddedResource(gpuRenderer, vertexShaderResource, "vertexMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX, numUniformBuffers: 1);
        SDL_GPUShader* fragmentShader = GpuShader.CreateFromEmbeddedResource(gpuRenderer, fragmentShaderResource, "fragmentMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT, numSamplers: fragmentSamplers, numUniformBuffers: fragmentUniformBuffers);

        if (vertexShader == null || fragmentShader == null)
        {
            throw new InvalidOperationException($"Shader creation failed: {SDL_GetError()}");
        }

        var vertexBufferDescriptions = stackalloc SDL_GPUVertexBufferDescription[1];
        vertexBufferDescriptions[0] = GpuVertexLayout.CreateVertexBufferDescription(VertexPcttb.Components);

        var vertexAttributesManaged = GpuVertexLayout.CreateVertexAttributes(VertexPcttb.Components);

        var colorTargetDescriptions = stackalloc SDL_GPUColorTargetDescription[1];
        colorTargetDescriptions[0] = new SDL_GPUColorTargetDescription
        {
            format = SDL_GetGPUSwapchainTextureFormat(_device, window),
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

        fixed (SDL_GPUVertexAttribute* vertexAttributes = vertexAttributesManaged)
        {
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
                    num_vertex_attributes = (uint)vertexAttributesManaged.Length,
                },
                target_info = new SDL_GPUGraphicsPipelineTargetInfo
                {
                    color_target_descriptions = colorTargetDescriptions,
                    num_color_targets = 1,
                },
            };

            Pipeline = SDL_CreateGPUGraphicsPipeline(_device, &pipelineCreateInfo);
        }

        if (Pipeline == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUGraphicsPipeline failed: {SDL_GetError()}");
        }

        SDL_ReleaseGPUShader(_device, vertexShader);
        SDL_ReleaseGPUShader(_device, fragmentShader);

        var linearSamplerCreateInfo = new SDL_GPUSamplerCreateInfo
        {
            min_filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
            mag_filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
            mipmap_mode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,
            address_mode_u = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_v = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_w = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        };
        LinearSampler = SDL_CreateGPUSampler(_device, &linearSamplerCreateInfo);

        if (LinearSampler == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUSampler failed: {SDL_GetError()}");
        }

        var pointSamplerCreateInfo = new SDL_GPUSamplerCreateInfo
        {
            min_filter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST,
            mag_filter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST,
            mipmap_mode = SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,
            address_mode_u = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_v = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
            address_mode_w = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        };
        PointSampler = SDL_CreateGPUSampler(_device, &pointSamplerCreateInfo);

        if (PointSampler == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUSampler failed: {SDL_GetError()}");
        }
    }

    public virtual void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures, Matrix4x4 transform)
    {
        SDL_BindGPUGraphicsPipeline(renderPass, Pipeline);

        var targetSize = GpuRenderer.TargetSize;
        var offset = GpuRenderer.RenderStateProvider.Offset;
        var scale = GpuRenderer.RenderStateProvider.Scale;

        SDL_GPUTexture* texture = null;
        Vector4 globalColor = new Vector4(1f, 1f, 1f, 1f);

        if (textures.Length != 0)
        {
            if (GpuRenderer.CurrentPass == RenderPass.Glow)
            {
                var glowTexture = textures.Length > 1 ? textures[1] : null;
                if (glowTexture == null)
                {
                    texture = textures[0];
                    globalColor = new Vector4(0f, 0f, 0f, 0f);
                }
                else
                {
                    texture = glowTexture;
                    globalColor = new Vector4(1f, 1f, 1f, 1f);
                }
            }
            else
            {
                texture = textures[0];
                globalColor = new Vector4(1f, 1f, 1f, 1f);
            }
        }

        var screenUniforms = new ScreenUniforms
        {
            ScreenSize = new Vector4(targetSize.Width, targetSize.Height, 0f, 0f),
            OffsetScale = new Vector4(offset.X, offset.Y, scale, 0f),
            GlobalColor = globalColor,
            Transform = transform,
        };
        SDL_PushGPUVertexUniformData(commandBuffer, 0, (IntPtr)(&screenUniforms), (uint)sizeof(ScreenUniforms));

        if (textures.Length == 0)
        {
            return;
        }

        var sampler = GpuRenderer.RenderStateProvider.TextureFilter == TextureFilter.Point ? PointSampler : LinearSampler;

        var samplerBindings = stackalloc SDL_GPUTextureSamplerBinding[1];
        samplerBindings[0] = new SDL_GPUTextureSamplerBinding { texture = texture, sampler = sampler };

        SDL_BindGPUFragmentSamplers(renderPass, 0, samplerBindings, 1);
    }

    public virtual void Dispose()
    {
        SDL_ReleaseGPUSampler(_device, LinearSampler);
        SDL_ReleaseGPUSampler(_device, PointSampler);
        SDL_ReleaseGPUGraphicsPipeline(_device, Pipeline);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenUniforms
    {
        public Vector4 ScreenSize;
        public Vector4 OffsetScale; // xy = offset, z = scale, w unused
        public Vector4 GlobalColor;
        public Matrix4x4 Transform;
    }
}
