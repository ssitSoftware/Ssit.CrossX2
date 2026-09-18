using Ssit.CrossX2.Graphics.Font;
using Ssit.CrossX2.IO;
using Ssit.CrossX2.IoC;
using Ssit.CrossX2.Text;

namespace Ssit.CrossX2.Graphics.Internal;

// ReSharper disable once ClassNeverInstantiated.Global
internal class GraphicGlyphFont : GlyphFont, IGlyphFont
{
    public ITexture OutlineSheet { get; }
    public ITexture FontSheet { get; }

    public int LineSize => Metrics.LineHeight;
    
    public GraphicGlyphFont(string path, IFilesProvider filesProvider, IIoCContainer iocContainer)
    {
        using var stream = filesProvider.Open(path);
        Load(stream);

        var sheetPath = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path)) + ".png";
        (FontSheet, OutlineSheet) = TextureHelper.LoadComplexSheet(filesProvider, iocContainer, sheetPath);
    }

    public void Dispose()
    {
        FontSheet?.Dispose();
        OutlineSheet?.Dispose();
    }

    public Size TextSize(TextSource text, TextSpacing spacing) => GlyphFontRenderer.MeasureText(this, text, spacing);
}