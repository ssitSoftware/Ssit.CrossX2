using System.Numerics;
using Ssit.CrossX2.Framework.Text;

namespace Ssit.CrossX2.Framework.Graphics;

public interface ISmartTextRenderer
{
    void PrepareFont(string fontName, float size);
    
    void DrawText(TextSource text, Vector2 position, ContentAlign align = ContentAlign.Left, float scale = 1,
        RgbaColor? color = null, TextSpacing spacing = TextSpacing.Normal, RgbaColor? outlineColor = null,
        int lineSpacing = 0,
        TextRenderingContext context = null);
    
    void DrawText(TextSource text, RectangleF position, ContentAlign align = ContentAlign.Left, float scale = 1,
        RgbaColor? color = null, TextSpacing spacing = TextSpacing.Normal, float paragraphSpacing = -1,
        RgbaColor? outlineColor = null, int lineSpacing = 0, TextRenderingContext context = null);
}