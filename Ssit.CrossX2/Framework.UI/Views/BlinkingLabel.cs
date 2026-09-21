namespace Ssit.CrossX2.Framework.UI.Views;

public class BlinkingLabel : Label, IBlinkingView
{
    public float? VisibleTime { get; set; }
    public float? HiddenTime { get; set; }
}