namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Parameters for constructing an <see cref="IVertexBuffer"/> through the IoC container.
/// </summary>
public class CreateVertexBufferParameters
{
    /// <summary>
    /// Number of vertices the buffer holds.
    /// </summary>
    public int Count { get; set; }
    /// <summary>
    /// Data stored per vertex (must match the vertex struct passed to SetData).
    /// </summary>
    public VertexComponents Components { get; set; }
}