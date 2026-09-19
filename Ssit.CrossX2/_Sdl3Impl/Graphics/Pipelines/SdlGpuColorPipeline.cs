using System.Numerics;
using System.Runtime.InteropServices;
using SDL;
using Ssit.CrossX2.Graphics;
using static SDL.SDL3;

namespace Ssit.CrossX2._Sdl3Impl.Graphics.Pipelines;

internal unsafe class SdlGpuColorPipeline : ISdlGpuPipeline
{
    private readonly SdlGpuRenderer _gpuRenderer;
    private readonly SDL_GPUDevice* _device;

    public SDL_GPUGraphicsPipeline* Pipeline { get; }

    public SdlGpuColorPipeline(SdlHandles handles, SdlGpuRenderer gpuRenderer, SDL_GPUPrimitiveType primitiveType)
    {
        _gpuRenderer = gpuRenderer;
        _device = handles.GpuDevice;

        SDL_GPUShader* vertexShader = GpuShader.CreateFromEmbeddedResource(gpuRenderer, "Pipelines.Shaders.Color.vert", "vertexMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX, numUniformBuffers: 1);
        SDL_GPUShader* fragmentShader = GpuShader.CreateFromEmbeddedResource(gpuRenderer, "Pipelines.Shaders.Color.frag", "fragmentMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT);

        if (vertexShader == null || fragmentShader == null)
            throw new InvalidOperationException($"Shader creation failed: {SDL_GetError()}");

        var vertexBufferDescriptions = stackalloc SDL_GPUVertexBufferDescription[1];
        vertexBufferDescriptions[0] = GpuVertexLayout.CreateVertexBufferDescription(VertexPct2D.Components);

        var vertexAttributesManaged = GpuVertexLayout.CreateVertexAttributes(VertexPct2D.Components);

        var colorTargetDescriptions = stackalloc SDL_GPUColorTargetDescription[1];
        colorTargetDescriptions[0] = new SDL_GPUColorTargetDescription
        {
            format = SDL_GetGPUSwapchainTextureFormat(handles.GpuDevice, handles.Window),
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
                primitive_type = primitiveType,
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
            throw new InvalidOperationException($"SDL_CreateGPUGraphicsPipeline failed: {SDL_GetError()}");

        SDL_ReleaseGPUShader(_device, vertexShader);
        SDL_ReleaseGPUShader(_device, fragmentShader);
    }

    public void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures)
    {
        SDL_BindGPUGraphicsPipeline(renderPass, Pipeline);

        var targetSize = _gpuRenderer.TargetSize;
        var offset = _gpuRenderer.RenderStateProvider.Offset;
        var scale = _gpuRenderer.RenderStateProvider.Scale;

        var screenUniforms = new ScreenUniforms
        {
            ScreenSize = new Vector4(targetSize.Width, targetSize.Height, 0f, 0f),
            OffsetScale = new Vector4(offset.X, offset.Y, scale, 0f),
        };
        SDL_PushGPUVertexUniformData(commandBuffer, 0, (IntPtr)(&screenUniforms), (uint)sizeof(ScreenUniforms));
    }

    public void Dispose()
    {
        SDL_ReleaseGPUGraphicsPipeline(_device, Pipeline);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenUniforms
    {
        public Vector4 ScreenSize;
        public Vector4 OffsetScale; // xy = offset, z = scale, w unused
    }
}