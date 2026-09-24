using System.Numerics;
using System.Runtime.InteropServices;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Pipelines;

internal unsafe class SdlGpuColorPipeline : ISdlGpuPipeline
{
    protected readonly SdlGpuRenderer GpuRenderer;
    private readonly SDL_GPUDevice* _device;

    private readonly SDL_GPUGraphicsPipeline*[] _pipelines = new SDL_GPUGraphicsPipeline*[SdlGpuBlendStateFactory.AllModes.Length];

    public SdlGpuColorPipeline(SdlHandles handles, SdlGpuRenderer gpuRenderer, SDL_GPUPrimitiveType primitiveType)
        : this(handles, gpuRenderer, primitiveType, "Pipelines.Color.vert", "Pipelines.Color.frag", fragmentUniformBuffers: 0)
    {
    }

    protected SdlGpuColorPipeline(SdlHandles handles, SdlGpuRenderer gpuRenderer, SDL_GPUPrimitiveType primitiveType,
        string vertexShaderResource, string fragmentShaderResource, int fragmentUniformBuffers)
    {
        GpuRenderer = gpuRenderer;
        _device = handles.GpuDevice;

        SDL_GPUShader* vertexShader = GpuShader.CreateFromEmbeddedResource(gpuRenderer, vertexShaderResource, "vertexMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX, numUniformBuffers: 1);
        SDL_GPUShader* fragmentShader = GpuShader.CreateFromEmbeddedResource(gpuRenderer, fragmentShaderResource, "fragmentMain", SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT, numUniformBuffers: fragmentUniformBuffers);

        if (vertexShader == null || fragmentShader == null)
            throw new InvalidOperationException($"Shader creation failed: {SDL_GetError()}");

        var vertexBufferDescriptions = stackalloc SDL_GPUVertexBufferDescription[1];
        vertexBufferDescriptions[0] = GpuVertexLayout.CreateVertexBufferDescription(VertexPcttb.Components);

        var vertexAttributesManaged = GpuVertexLayout.CreateVertexAttributes(VertexPcttb.Components);

        var swapchainFormat = SDL_GetGPUSwapchainTextureFormat(handles.GpuDevice, handles.Window);

        var colorTargetDescriptions = stackalloc SDL_GPUColorTargetDescription[1];

        fixed (SDL_GPUVertexAttribute* vertexAttributes = vertexAttributesManaged)
        {
            foreach (var blendMode in SdlGpuBlendStateFactory.AllModes)
            {
                colorTargetDescriptions[0] = new SDL_GPUColorTargetDescription
                {
                    format = swapchainFormat,
                    blend_state = SdlGpuBlendStateFactory.Create(blendMode),
                };

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

                var pipeline = SDL_CreateGPUGraphicsPipeline(_device, &pipelineCreateInfo);

                if (pipeline == null)
                    throw new InvalidOperationException($"SDL_CreateGPUGraphicsPipeline failed: {SDL_GetError()}");

                _pipelines[(int)blendMode] = pipeline;
            }
        }

        SDL_ReleaseGPUShader(_device, vertexShader);
        SDL_ReleaseGPUShader(_device, fragmentShader);
    }

    public virtual void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures, Matrix4x4 transform)
    {
        var pipeline = _pipelines[(int)GpuRenderer.RenderStateProvider.BlendMode];
        SDL_BindGPUGraphicsPipeline(renderPass, pipeline);

        var targetSize = GpuRenderer.TargetSize;

        var screenUniforms = new ScreenUniforms
        {
            ScreenSize = new Vector4(targetSize.Width, targetSize.Height, 0f, 0f),
            Transform = transform,
        };
        SDL_PushGPUVertexUniformData(commandBuffer, 0, (IntPtr)(&screenUniforms), (uint)sizeof(ScreenUniforms));
    }

    public virtual void Dispose()
    {
        foreach (var pipeline in _pipelines)
        {
            if (pipeline != null)
            {
                SDL_ReleaseGPUGraphicsPipeline(_device, pipeline);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenUniforms
    {
        public Vector4 ScreenSize;
        public Matrix4x4 Transform;
    }
}