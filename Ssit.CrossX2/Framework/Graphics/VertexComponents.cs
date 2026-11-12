namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Data a vertex contains.
/// </summary>
[Flags]
public enum VertexComponents
{
    /// <summary>
    /// Position.
    /// </summary>
    Position = 1,
    /// <summary>
    /// Vertex color.
    /// </summary>
    Color = 2,
    /// <summary>
    /// Texture coordinates.
    /// </summary>
    Texture = 4
}