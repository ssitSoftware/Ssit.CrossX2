using System.Numerics;
using SDL;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Renderers;

using static SDL.SDL3;

namespace Ssit.CrossX2._Sdl3Impl.Graphics;

internal unsafe class SdlGpuPrimitiveRenderer(SdlGpuRenderer renderer): IPrimitiveRenderer
{
    private readonly SDL_GPUTexture*[] _sdlGpuTextures =  new SDL_GPUTexture*[3];

    public void RenderVertices(PrimitiveType type, IVertexBuffer vertices, int start, int count, ITexture texture = null)
    {
        var sdlBuffer = vertices as SdlGpuVertexBuffer;

        if (sdlBuffer is null)
            throw new ArgumentException($"{nameof(vertices)} must be a {nameof(SdlGpuVertexBuffer)}", nameof(vertices));

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

        renderer.BeginNewRenderPass();

        var renderPass = renderer.CurrentGpuRenderPass;

        int strideBytes = GetStrideBytes(vertices.Components);

        var vertexBufferBinding = new SDL_GPUBufferBinding
        {
            buffer = sdlBuffer.Handle,
            offset = (uint)(start * strideBytes),
        };
        SDL_BindGPUVertexBuffers(renderPass, 0, &vertexBufferBinding, 1);

        SDL_DrawGPUPrimitives(renderPass, (uint)count, 1, 0, 0);
    }

    private static int GetStrideBytes(VertexComponents components)
    {
        int stride = 0;

        if (components.HasFlag(VertexComponents.Position))
            stride += sizeof(Vector2);

        if (components.HasFlag(VertexComponents.Color))
            stride += sizeof(RgbaColor);

        if (components.HasFlag(VertexComponents.Texture))
            stride += sizeof(Vector2);

        return stride;
    }
}