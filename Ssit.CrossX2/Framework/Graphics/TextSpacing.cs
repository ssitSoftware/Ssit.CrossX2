namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Letter spacing. The value relative to Normal (50) adds (value - 50) / 50 of a whitespace width between characters.
/// </summary>
public enum TextSpacing
{
    /// <summary>
    /// Slightly tighter than normal.
    /// </summary>
    Condensed = 45,
    /// <summary>
    /// Font's native spacing.
    /// </summary>
    Normal = 50,
    /// <summary>
    /// Slightly wider.
    /// </summary>
    Expanded = 58,
    /// <summary>
    /// Wide.
    /// </summary>
    SuperExpanded = 66,
    /// <summary>
    /// Very wide.
    /// </summary>
    UltraExpanded = 74
}