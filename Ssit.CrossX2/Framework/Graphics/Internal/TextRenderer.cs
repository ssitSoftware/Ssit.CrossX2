using System.Numerics;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Graphics.Renderers;
using Ssit.CrossX2.Framework.Text;

namespace Ssit.CrossX2.Framework.Graphics.Internal;

public class TextRenderer(IRenderQueue renderQueue): ITextRenderer
{
    private readonly IQuadsRenderer _quadsRenderer = new QuadsRenderer(renderQueue);
    
    public void DrawText(IFont font, TextSource text, Vector2 position, ContentAlign align = ContentAlign.Left, float scale = 1,
        RgbaColor? color = null, TextSpacing spacing = TextSpacing.Normal, RgbaColor? outlineColor = null, int lineSpacing = 1,
        TextRenderingContext context = null)
    {
        if (font is not IGlyphFont glyphFont)
        {
            return;
        }
        
        GlyphFontRenderer.RenderText(_quadsRenderer, glyphFont, text, position, align, scale, color ?? RgbaColor.White, outlineColor ?? RgbaColor.Black, spacing, lineSpacing, context);
    }

    public void DrawText(IFont font, TextSource text, RectangleF position, ContentAlign align = ContentAlign.Left, float scale = 1,
        RgbaColor? color = null, TextSpacing spacing = TextSpacing.Normal, float paragraphSpacing = -1,
        RgbaColor? outlineColor = null, int lineSpacing = 0, TextRenderingContext context = null)
    {
        if (font is not IGlyphFont glyphFont)
        {
            return;
        }

        if (paragraphSpacing < 0)
        {
            paragraphSpacing = glyphFont.Metrics.LineHeight / 4f;
        }

        GlyphFontRenderer.RenderText(_quadsRenderer, glyphFont, text, position, align, scale, color ?? RgbaColor.White,
            outlineColor ?? RgbaColor.Black, spacing, paragraphSpacing, lineSpacing, context);
    }
}