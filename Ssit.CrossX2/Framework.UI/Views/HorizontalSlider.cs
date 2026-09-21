using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class HorizontalSlider: View
{
    public SharedValue<int> Value { get; set; }
    public int Max { get; set; } = 10;
    public int Min { get; set; } = 0;
    
    public SliderTemplate Template { get; set; }
    
    public ColorWrapper? OutlineColor { get; set; }
    public ColorWrapper? ForegroundColor { get; set; }
}