namespace Ssit.CrossX2.Framework.Graphics;

public interface IVertexBuffer: IDisposable
{
    VertexComponents Components { get; }
    int Count { get; }
    void SetData<TVertex>( TVertex[] data ) where TVertex : unmanaged;
}