using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class NinePatchCard: View
{
    public ColorWrapper[] Colors { get; set; }
    public IImageSource<TextureArray> Source { get; set; }
    public RectangleF? SourceCenterRect { get; set; }
    public TextureFilter? Filter { get; set; }
} 