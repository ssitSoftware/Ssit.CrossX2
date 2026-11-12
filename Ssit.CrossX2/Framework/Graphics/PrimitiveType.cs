namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// How vertices are assembled into primitives.
/// </summary>
public enum PrimitiveType
{
    /// <summary>
    /// Every two vertices form a line.
    /// </summary>
    Lines,
    /// <summary>
    /// Every three vertices form a triangle.
    /// </summary>
    Triangles
}