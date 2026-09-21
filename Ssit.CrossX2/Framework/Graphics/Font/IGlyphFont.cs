using Ssit.CrossX2.Framework.Graphics.Internal;

namespace Ssit.CrossX2.Framework.Graphics.Font;

public interface IGlyphFont: IFont, IDisposable
{
    GlyphFont.FontMetrics Metrics { get; }
    Glyph GetGlyph(char c);
    ITexture FontSheet { get; }
    ITexture OutlineSheet { get; }
}