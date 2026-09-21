using Ssit.CrossX2.Framework.UI.Parameters;

namespace Ssit.CrossX2.Framework.UI.Views;

public class LabelButtonEx: LabelButton
{
    public Length? FocusBevel { get; set; }
    public Length? FocusWaveAmplitude { get; set; }
    public float? FocusWaveFrequency { get; set; }
    public ColorWrapper FocusedLowColor { get; set; }
    public bool? AnimateBevel { get; set; }
}