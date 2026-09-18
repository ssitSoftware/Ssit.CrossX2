namespace Ssit.CrossX2.Graphics;

public interface IVertexBuffer: IDisposable
{
    VertexComponents Components { get; }
    int Count { get; }
    void SetData<TVertex>( TVertex[] data ) where TVertex : unmanaged;
}