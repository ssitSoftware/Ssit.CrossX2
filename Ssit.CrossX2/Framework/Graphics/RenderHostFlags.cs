namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Render host scaling mode and effects. Without a match flag the design size is rendered at MinScale.
/// </summary>
[Flags]
public enum RenderHostFlags
{
    /// <summary>
    /// Scale so the width matches the window; the height follows the window's aspect ratio.
    /// </summary>
    MatchWidth = 1,
    /// <summary>
    /// Scale so the height matches the window; the width follows the window's aspect ratio.
    /// </summary>
    MatchHeight = 2,
    /// <summary>
    /// Keep exactly the design aspect ratio, scaled to fit the window (letterboxed).
    /// </summary>
    ExactSize = MatchWidth | MatchHeight,
    /// <summary>
    /// Use integer scales only and point filtering for crisp pixel art.
    /// </summary>
    PixelPerfect = 8,
    /// <summary>
    /// Render an additional glow pass composited over the frame.
    /// </summary>
    EnableGlowPass = 16,
    /// <summary>
    /// Apply the CRT monitor simulation effect when presenting.
    /// </summary>
    EnableCrtSimulation = 32
    
}