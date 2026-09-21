using System.Numerics;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Text;

namespace Ssit.CrossX2.Framework.Graphics.Internal;

internal class BitmapFont: IGlyphFont
{
    public string Name { get;}
    public int Size { get;}
    public int LineSize { get;}

    public ITexture FontSheet { get; }
    public ITexture OutlineSheet => null;

    public GlyphFont.FontMetrics Metrics { get; }

    private readonly Dictionary<char, Glyph> _glyphs = new();
    private readonly Glyph _emptyGlyph;

    public BitmapFont(string name, Size size, ITexture fontSheet, char firstChar, char lastChar)
    {
        Name = name;
        Size = size.Height;
        LineSize = size.Height;
        FontSheet = fontSheet;
        
        _emptyGlyph = new Glyph(' ', Rectangle.Empty, Vector2.Zero, size.Width);

        var index = 0;
        var columns = fontSheet.Size.Width / size.Width;
        
        for (var c = firstChar; c <= lastChar; c++)
        {
            var rect = new Rectangle(index % columns * size.Width, index / columns * size.Height, size.Width, size.Height);
            _glyphs[c] = new Glyph(c, rect, Vector2.Zero, size.Width);
            ++index;
        }
        
        Metrics = new GlyphFont.FontMetrics(size.Height, size.Height, 0, 0, size.Height, size.Width);
    }
    
    public Size TextSize(TextSource text, TextSpacing spacing) => GlyphFontRenderer.MeasureText(this, text, spacing);

    public Glyph GetGlyph(char c) => _glyphs.GetValueOrDefault(c, _emptyGlyph);

    public void Dispose() => FontSheet?.Dispose();
}