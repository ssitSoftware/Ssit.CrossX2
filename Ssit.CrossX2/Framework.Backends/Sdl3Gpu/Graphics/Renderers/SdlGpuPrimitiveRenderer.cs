using System.Numerics;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Renderers;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Renderers;

internal unsafe class SdlGpuPrimitiveRenderer(SdlGpuRenderer renderer, ISdlGpuPipelineManager pipelineManager): IPrimitiveRenderer
{
    private readonly SDL_GPUTexture*[] _sdlGpuTextures =  new SDL_GPUTexture*[3];

    internal void RenderVertices(PrimitiveType type, VertexComponents components, SDL_GPUBuffer* sdlBuffer, int start, int count, ITexture texture = null, Matrix4x4? transform = null)
    {
        if (texture is ISdlGpuTexture sdlTexture)
        {
            _sdlGpuTextures[0] = sdlTexture.GetMap(TextureMaps.Diffuse);
            _sdlGpuTextures[1] = sdlTexture.GetMap(TextureMaps.Glow);
            _sdlGpuTextures[2] = sdlTexture.GetMap(TextureMaps.Normal);
        }
        else
        {
            _sdlGpuTextures[0] = null;
            _sdlGpuTextures[1] = null;
            _sdlGpuTextures[2] = null;
        }

        if (type == PrimitiveType.Lines && texture != null)
        {
            throw new ArgumentException($"Cannot draw lines with texture", nameof(type));
        }

        var pipeline = pipelineManager.GetProperPipeline(_sdlGpuTextures[0] != null, type);
        
        var commandBuffer = renderer.CommandBuffer;
        var renderPass = renderer.CurrentGpuRenderPass;
        
        pipeline.Bind(commandBuffer, renderPass, _sdlGpuTextures, transform ?? Matrix4x4.Identity);
        
        int strideBytes = GpuVertexLayout.GetStrideBytes(components);

        var vertexBufferBinding = new SDL_GPUBufferBinding
        {
            buffer = sdlBuffer,
            offset = (uint)(start * strideBytes),
        };

        SDL_BindGPUVertexBuffers(renderPass, 0, &vertexBufferBinding, 1);
        SDL_DrawGPUPrimitives(renderPass, (uint)count, 1, 0, 0);
    }
    
    public void RenderVertices(PrimitiveType type, IVertexBuffer vertices, int start, int count, ITexture texture = null, Matrix4x4? transform = null)
    {
        var sdlBuffer = vertices as SdlGpuVertexBuffer;

        if (sdlBuffer is null)
        {
            throw new ArgumentException($"{nameof(vertices)} must be a {nameof(SdlGpuVertexBuffer)}", nameof(vertices));
        }
        
        RenderVertices(type, vertices.Components, sdlBuffer.Handle, start, count, texture, transform);
    }
}