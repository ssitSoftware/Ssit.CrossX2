using System.Numerics;
using System.Runtime.InteropServices;
using SDL;
using Ssit.CrossX2.Graphics;
using static SDL.SDL3;

namespace Ssit.CrossX2._Sdl3Impl.Graphics.Pipelines;

internal unsafe class SdlSdlGpuTexturePipeline : ISdlGpuPipeline
{
    protected readonly SdlGpuRenderer GpuRenderer;

    public VertexComponents VertexFormat => VertexComponents.Position | VertexComponents.Texture | VertexComponents.Color;
    
    private readonly SDL_GPUDevice* _device;
    public SDL_GPUGraphicsPipeline* Pipeline { get; }
    public SDL_GPUSampler* Sampler { get; }
    
    public SdlSdlGpuTexturePipeline(SdlHandles handles, SdlGpuRenderer gpuRenderer)
        : this(handles.GpuDevice, handles.Window, "Shaders.Screen.vertPct.metal", "Shaders.Screen.frag.metal", fragmentUniformBuffers: 0)
    {
        GpuRenderer = gpuRenderer;
    }

    protected SdlSdlGpuTexturePipeline(SDL_GPUDevice* device, SDL_Window* window, string vertexShaderResource, string fragmentShaderResource, int fragmentUniformBuffers)
    {
        _device = device;

        SDL_GPUShader* vertexShader = GpuShader.CreateFromEmbeddedResource(device, vertexShaderResource, "vertexMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX, numUniformBuffers: 1);
        SDL_GPUShader* fragmentShader = GpuShader.CreateFromEmbeddedResource(device, fragmentShaderResource, "fragmentMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT, numSamplers: 1, numUniformBuffers: fragmentUniformBuffers);

        if (vertexShader == null || fragmentShader == null)
        {
            throw new InvalidOperationException($"Shader creation failed: {SDL_GetError()}");
        }

        var vertexBufferDescriptions = stackalloc SDL_GPUVertexBufferDescription[1];
        vertexBufferDescriptions[0] = GpuVertexLayout.CreateVertexBufferDescription(VertexFormat);

        var vertexAttributesManaged = GpuVertexLayout.CreateVertexAttributes(VertexFormat);

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

            Pipeline = SDL_CreateGPUGraphicsPipeline(device, &pipelineCreateInfo);
        }

        if (Pipeline == null)
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
        Sampler = SDL_CreateGPUSampler(device, &samplerCreateInfo);

        if (Sampler == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUSampler failed: {SDL_GetError()}");
        }
    }

    public virtual void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures)
    {
        SDL_BindGPUGraphicsPipeline(renderPass, Pipeline);

        var targetSize = GpuRenderer.TargetSize;
        var offset = GpuRenderer.RenderStateProvider.Offset;
        var scale = GpuRenderer.RenderStateProvider.Scale;
        
        var screenUniforms = new ScreenUniforms
        {
            ScreenSize = new Vector4(targetSize.Width, targetSize.Height, 0f, 0f),
            OffsetScale = new Vector4(offset.X, offset.Y, scale, 0f),
        };
        SDL_PushGPUVertexUniformData(commandBuffer, 0, (IntPtr)(&screenUniforms), (uint)sizeof(ScreenUniforms));

        if (textures.Length == 0)
        {
            return;
        }
        
        var  texture =  GpuRenderer.CurrentPass == RenderPass.Glow ? textures[1] : textures[0];
        var samplerBindings = stackalloc SDL_GPUTextureSamplerBinding[1];
        samplerBindings[0] = new SDL_GPUTextureSamplerBinding { texture = texture, sampler = Sampler };

        SDL_BindGPUFragmentSamplers(renderPass, 0, samplerBindings, (uint)textures.Length);
    }

    public virtual void Dispose()
    {
        SDL_ReleaseGPUSampler(_device, Sampler);
        SDL_ReleaseGPUGraphicsPipeline(_device, Pipeline);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenUniforms
    {
        public Vector4 ScreenSize;
        public Vector4 OffsetScale; // xy = offset, z = scale, w unused
    }
}
