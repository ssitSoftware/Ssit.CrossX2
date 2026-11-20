using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class HorizontalSlider: View
{
    public SharedValue<int> Value { get; set; }
    public int Max { get; set; } = 10;
    public int Min { get; set; } = 0;
    
    public SliderTemplate Template { get; set; }

    /// <summary>
    /// Width (in design pixels) of the outline baked into the slider graphics. Text outlines are drawn outside
    /// the text bounds, so to line the slider up with the text around it, the track is extended by this width
    /// on both sides and its outline lies outside the slider bounds (the thumb travels along the extended track).
    /// The layout size and the touchable area stay the slider bounds. 0 (default) - the graphics fit in the bounds.
    /// </summary>
    public float WithOutlineWidth { get; set; }
    
    public ColorWrapper? OutlineColor { get; set; }
    public ColorWrapper? ForegroundColor { get; set; }
}