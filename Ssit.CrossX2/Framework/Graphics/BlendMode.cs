namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// How drawn pixels are combined with the render target.
/// </summary>
public enum BlendMode
{
    /// <summary>
    /// Overwrite the target (no blending).
    /// </summary>
    None,
    /// <summary>
    /// Standard transparency using the source alpha.
    /// </summary>
    AlphaBlend,
    /// <summary>
    /// Add the source color to the target (lights, glows).
    /// </summary>
    Additive,
    /// <summary>
    /// Multiply the target by the source color (shadows, tinting).
    /// </summary>
    Multiply
}