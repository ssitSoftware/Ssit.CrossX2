using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Read-only view of the current render state managed by <see cref="IStateManager"/>.
/// </summary>
public interface IRenderStateProvider
{
    /// <summary>
    /// Current world transform (accumulated translations and scales).
    /// </summary>
    Matrix4x4 Transform { get; }
    /// <summary>
    /// Current blend mode.
    /// </summary>
    BlendMode BlendMode { get; }
    /// <summary>
    /// Current texture sampling filter.
    /// </summary>
    TextureFilter TextureFilter { get; }
    /// <summary>
    /// Current clipping rectangle, null when drawing is not clipped.
    /// </summary>
    RectangleF? ClipRect { get; }
    /// <summary>
    /// Current accumulated scale factor.
    /// </summary>
    float Scale { get;}
    /// <summary>
    /// Color all drawing is multiplied with.
    /// </summary>
    RgbaColor TintColor { get; }
}