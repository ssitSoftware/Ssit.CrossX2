using System.Numerics;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Text;

namespace Ssit.CrossX2.Framework.Graphics;

internal class SmartTextRenderer(IFontsManager fontsManager, IRenderer renderer): ISmartTextRenderer
{
    private string _fontName;
    private float _size;

    private TextRenderingContext _context = new();
    
    public void PrepareFont(string fontName, float size)
    {
        _fontName = fontName;
        _size = size;
    }
    
    public void DrawText(TextSource text, Vector2 position, ContentAlign align = ContentAlign.Left,
        float scale = 1, RgbaColor? color = null,
        TextSpacing spacing = TextSpacing.Normal, RgbaColor? outlineColor = null, int lineSpacing = 0, TextRenderingContext context = null)
    {
        var font =  fontsManager.GetFont(_fontName, _size * renderer.RenderStateProvider.Scale);
        scale /= renderer.RenderStateProvider.Scale;
        renderer.TextRenderer.DrawText(font, text, position, align, scale, color, spacing, outlineColor, lineSpacing, context);
    }

    public void DrawText(TextSource text, RectangleF position, ContentAlign align = ContentAlign.Left,
        float scale = 1, RgbaColor? color = null,
        TextSpacing spacing = TextSpacing.Normal, float paragraphSpacing = -1, RgbaColor? outlineColor = null,
        int lineSpacing = 0,
        TextRenderingContext context = null)
    {
        var font =  fontsManager.GetFont(_fontName, _size * renderer.RenderStateProvider.Scale);
        scale /= renderer.RenderStateProvider.Scale;
        renderer.TextRenderer.DrawText(font, text, position, align, scale, color, spacing, paragraphSpacing, outlineColor, lineSpacing, context);
    }
}