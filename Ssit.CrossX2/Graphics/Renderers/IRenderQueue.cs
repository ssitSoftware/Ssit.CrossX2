namespace Ssit.CrossX2.Graphics.Renderers;

public interface IRenderQueue
{
    void PushLine(VertexPct p1, VertexPct p2);
    void PushTriangle(VertexPct p1, VertexPct p2, VertexPct p3, ITexture texture = null);
    void PushVertices(PrimitiveType type, IVertexBuffer vertices, int start, int count, ITexture texture = null);
}