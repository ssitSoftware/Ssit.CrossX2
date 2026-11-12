namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Texture sampling when scaling.
/// </summary>
public enum TextureFilter
{
    /// <summary>
    /// Nearest pixel - crisp pixel art.
    /// </summary>
    Point,
    /// <summary>
    /// Bilinear interpolation - smooth.
    /// </summary>
    Linear
}