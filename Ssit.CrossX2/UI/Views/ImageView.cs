using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Views;

public class ImageView: Background
{
    public IImageSource<ITexture> Source { get; set; }
    public ContentAlign? ContentAlign { get; set; }
    public RgbaColor? TintColor { get; set; }
    public ImageScalingMode? Scaling { get; set; }
    public ImageTransform? Transform { get; set; }
    public TextureFilter? Filter { get; set; }
}