using SDL;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Renderers;

using static SDL.SDL3;

namespace Ssit.CrossX2._Sdl3Impl.Graphics;

internal unsafe class SdlGpuPrimitiveRenderer(SdlGpuRenderer renderer, SdlGpuPipelineManager pipelineManager): IPrimitiveRenderer
{
    private readonly SDL_GPUTexture*[] _sdlGpuTextures =  new SDL_GPUTexture*[3];

    internal void RenderVertices(PrimitiveType type, VertexComponents components, SDL_GPUBuffer* sdlBuffer, int start, int count, ITexture texture = null)
    {
        if (texture is SdlGpuTexture sdlTexture)
        {
            _sdlGpuTextures[0] = sdlTexture.GetMap(TextureMaps.Diffuse);
            _sdlGpuTextures[1] = sdlTexture.GetMap(TextureMaps.Glow);
            _sdlGpuTextures[2] = sdlTexture.GetMap(TextureMaps.NormalAndSpecular);
        }
        else
        {
            _sdlGpuTextures[0] = null;
            _sdlGpuTextures[1] = null;
            _sdlGpuTextures[2] = null;
        }
        
        var pipeline = pipelineManager.GetProperPipeline(texture != null);
        
        var commandBuffer = renderer.CommandBuffer;
        var renderPass = renderer.CurrentGpuRenderPass;
        
        pipeline.Bind(commandBuffer, renderPass, _sdlGpuTextures);
        
        int strideBytes = GpuVertexLayout.GetStrideBytes(components);

        var vertexBufferBinding = new SDL_GPUBufferBinding
        {
            buffer = sdlBuffer,
            offset = (uint)(start * strideBytes),
        };

        SDL_BindGPUVertexBuffers(renderPass, 0, &vertexBufferBinding, 1);
        SDL_DrawGPUPrimitives(renderPass, (uint)count, 1, 0, 0);
    }
    
    public void RenderVertices(PrimitiveType type, IVertexBuffer vertices, int start, int count, ITexture texture = null)
    {
        var sdlBuffer = vertices as SdlGpuVertexBuffer;

        if (sdlBuffer is null)
        {
            throw new ArgumentException($"{nameof(vertices)} must be a {nameof(SdlGpuVertexBuffer)}", nameof(vertices));
        }

        RenderVertices(type, vertices.Components, sdlBuffer.Handle, start, count, texture);
    }
}