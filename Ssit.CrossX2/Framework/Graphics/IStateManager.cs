using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Stack-based render state: transform, tint, blend mode, texture filter and clipping.
/// Wrap temporary changes in <see cref="SaveState"/> / <see cref="RestoreState"/>.
/// </summary>
public interface IStateManager
{
    /// <summary>
    /// Pushes the current state onto the stack.
    /// </summary>
    void SaveState();
    /// <summary>
    /// Pops the last saved state and makes it current.
    /// </summary>
    void RestoreState();
    /// <summary>
    /// Clears the stack and returns to the default state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Multiplies the current transform by a uniform scale.
    /// </summary>
    void Scale(float scale);
    /// <summary>
    /// Moves the origin of the current transform by offset.
    /// </summary>
    void Translate(Vector2 offset);
    /// <summary>
    /// Multiplies the current tint color by color.
    /// </summary>
    void Tint(RgbaColor color);
    /// <summary>
    /// Sets how drawn pixels are blended with the target.
    /// </summary>
    void SetBlendMode(BlendMode blendMode);
    /// <summary>
    /// Sets the sampling filter for textures.
    /// </summary>
    void SetTextureFilter(TextureFilter filter);
    /// <summary>
    /// Restricts drawing to clipRect (null removes clipping). By default the rectangle is intersected with the current one.
    /// </summary>
    void SetClipRect(RectangleF? clipRect, bool intersectExisting = true);
}