namespace Ssit.CrossX2.Graphics.Renderers;

public interface IPrimitiveRenderer
{
    void RenderVertices(PrimitiveType type, IVertexBuffer vertices, int start, int count, ITexture texture = null);
}