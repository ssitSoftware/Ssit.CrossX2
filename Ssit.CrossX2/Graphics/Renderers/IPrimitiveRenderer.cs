namespace CrossX2.Graphics.Renderers;

public interface IPrimitiveRenderer
{
    void RenderVertices<TVertex>(PrimitiveType type, ReadOnlySpan<TVertex> vertices, ITexture? texture = null) where TVertex : unmanaged;
    void RenderVertices<TVertex>(PrimitiveType type, IVertexBuffer vertices, int start, int count, ITexture? texture = null) where TVertex : unmanaged;
}