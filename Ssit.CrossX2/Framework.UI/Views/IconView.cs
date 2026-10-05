using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class IconView: View
{
    public string IconPath { get; set; }
    public ColorWrapper? OutlineColor { get; set; }
    public ColorWrapper? ForegroundColor { get; set; }
    public float Scale { get; set; } = 1;
    public ImageScalingMode? Scaling { get; set; }
    public ContentAlign? ContentAlign { get; set; }
}
