using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Components;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class StopwatchControl: View
{
    public SharedValue<DateTime?> StartTime { get; set; }
    public FontDesc Font { get; set; }
    public ColorWrapper TextColor { get; set; }
    public ColorWrapper OutlineColor { get; set; }
    public StopwatchTimeElements TimeTimeElements { get; set; } = StopwatchTimeElements.Minutes | StopwatchTimeElements.Seconds | StopwatchTimeElements.Milliseconds;
    public TextScaling Scaling { get; set; }
    public Thickness? Padding { get; set; }
    public ContentAlign? Align { get; set; }
}