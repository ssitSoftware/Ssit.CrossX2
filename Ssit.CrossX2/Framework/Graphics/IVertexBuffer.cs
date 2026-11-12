namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// GPU buffer of vertices for custom geometry.
/// </summary>
public interface IVertexBuffer: IDisposable
{
    /// <summary>
    /// Data stored per vertex.
    /// </summary>
    VertexComponents Components { get; }
    /// <summary>
    /// Number of vertices.
    /// </summary>
    int Count { get; }
    /// <summary>
    /// Uploads vertex data. TVertex must match <see cref="Components"/> (e.g. <see cref="VertexPct"/>).
    /// </summary>
    void SetData<TVertex>( TVertex[] data ) where TVertex : unmanaged;
}