using System.Numerics;
using Ssit.CrossX2.Framework.Text;

namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Text renderer that picks the font size for the current render scale, so text stays sharp when the scene is scaled.
/// Call <see cref="PrepareFont"/> first, then draw.
/// </summary>
public interface ISmartTextRenderer
{
    /// <summary>
    /// Selects the font and its logical size used by the following DrawText calls.
    /// </summary>
    void PrepareFont(string fontName, float size);
    
    /// <summary>
    /// Draws text anchored at a point; align says which part of the text is at position.
    /// </summary>
    void DrawText(TextSource text, Vector2 position, ContentAlign align = ContentAlign.Left, float scale = 1,
        RgbaColor? color = null, TextSpacing spacing = TextSpacing.Normal, RgbaColor? outlineColor = null,
        int lineSpacing = 0,
        TextRenderingContext context = null);
    
    /// <summary>
    /// Draws text wrapped and aligned inside a rectangle. paragraphSpacing below zero uses the default spacing.
    /// </summary>
    void DrawText(TextSource text, RectangleF position, ContentAlign align = ContentAlign.Left, float scale = 1,
        RgbaColor? color = null, TextSpacing spacing = TextSpacing.Normal, float paragraphSpacing = -1,
        RgbaColor? outlineColor = null, int lineSpacing = 0, TextRenderingContext context = null);
}