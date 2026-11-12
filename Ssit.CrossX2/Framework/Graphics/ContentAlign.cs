namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Text/content alignment flags. Combine one horizontal value (Left, Center, Right) with one vertical value
/// (Top, VCenter, Bottom), optionally with Justified. Left and Top are the zero defaults.
/// </summary>
[Flags]
public enum ContentAlign
{
    /// <summary>
    /// Align to the left edge (default).
    /// </summary>
    Left = 0,
    /// <summary>
    /// Align to the top edge (default).
    /// </summary>
    Top = 0,
    /// <summary>
    /// Center horizontally.
    /// </summary>
    Center = 1,
    /// <summary>
    /// Center vertically.
    /// </summary>
    VCenter = 2,
    /// <summary>
    /// Align to the right edge.
    /// </summary>
    Right = 4,
    /// <summary>
    /// Align to the bottom edge.
    /// </summary>
    Bottom = 8,
    /// <summary>
    /// Stretch whitespace so wrapped lines fill the full width (the last line of a paragraph is not stretched).
    /// </summary>
    Justified = 16
}