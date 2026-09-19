namespace Ssit.CrossX2.Graphics.Renderers;

public interface IRenderQueue
{
    void PushLine(VertexPct2D p1, VertexPct2D p2);
    void PushTriangle(VertexPct2D p1, VertexPct2D p2, VertexPct2D p3, ITexture texture = null);
    void PushVertices(PrimitiveType type, IVertexBuffer vertices, int start, int count, ITexture texture = null);
}