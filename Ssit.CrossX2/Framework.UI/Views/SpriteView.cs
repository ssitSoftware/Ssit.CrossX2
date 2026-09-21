using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class SpriteView : View
{
    public string SpritePath { get; set; }
    public SharedString Sequence { get; set; }
    public float SpeedFactor { get; set; } = 1.0f;
    public Length? ImageAnchorX { get; set; }
    public Length? ImageAnchorY { get; set; }
    public int Scale { get; set; } = 1;
}
