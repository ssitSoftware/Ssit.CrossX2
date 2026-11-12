namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// A textured rectangle: where to draw it and which part of the texture to use.
/// </summary>
public readonly struct Quad(RectangleF target, RectangleF source)
{
    /// <summary>
    /// Destination rectangle.
    /// </summary>
    public readonly RectangleF Target = target;
    /// <summary>
    /// Source rectangle in the texture.
    /// </summary>
    public readonly RectangleF Source = source;
}