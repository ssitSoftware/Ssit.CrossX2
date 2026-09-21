using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.Text;

namespace Ssit.CrossX2.Framework.Graphics.Internal;

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