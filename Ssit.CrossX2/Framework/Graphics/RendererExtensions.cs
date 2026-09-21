using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Graphics.Internal;
using Ssit.CrossX2.Framework.Text;

namespace Ssit.CrossX2.Framework.Graphics;

public static class RendererExtensions
{
    public static TextRenderingContext CalculateMultilineText(this IFont font, TextSource text, TextSpacing spacing, float maxWidth, float paragraphSpacing, int lineSpacing = 0, TextRenderingContext context = null)
    {
        if (font is not IGlyphFont glyphFont) throw new NotSupportedException("This kind of font is not supported");

        context ??= new TextRenderingContext();
        
        GlyphFontRenderer.CalculateMultilineText(glyphFont, text, maxWidth, spacing, paragraphSpacing, lineSpacing, context);
        return context;
    }
    
    public static TextRenderingContext CalculateText(this IFont font, TextSource text, TextSpacing spacing, int lineSpacing = 0, TextRenderingContext context = null)
    {
        if (font is not IGlyphFont glyphFont) throw new NotSupportedException("This kind of font is not supported");

        context ??= new TextRenderingContext();
        
        GlyphFontRenderer.CalculateText(glyphFont, text, spacing, lineSpacing, context);
        return context;
    }
}