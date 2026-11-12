namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Mirroring applied when drawing an image.
/// </summary>
[Flags]
public enum ImageTransform
{
    /// <summary>
    /// Draw as is.
    /// </summary>
    None = 0,
    /// <summary>
    /// Mirror left-right.
    /// </summary>
    FlipHorizontal = 1,
    /// <summary>
    /// Mirror top-bottom.
    /// </summary>
    FlipVertical = 2
}