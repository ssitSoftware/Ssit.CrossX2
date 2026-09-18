using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.UI.Values;
using Ssit.CrossX2.UI.Views;

namespace Ssit.CrossX2.UI.Components;

public class StopwatchComponentParameters
{
    public FontDesc Font { get; set; }
    public ColorWrapper TextColor { get; set; }
    public ColorWrapper OutlineColor { get; set; }
    public TextScaling Scaling { get; set; }
    public StopwatchTimeElements TimeTimeElements { get; set; } = StopwatchTimeElements.Minutes | StopwatchTimeElements.Seconds | StopwatchTimeElements.Milliseconds;
    public SharedValue<DateTime?> StartTime { get; set; }
    public SharedBool ShouldDisplay { get; set; }
    public Thickness Padding { get; set; }
    public ContentAlign Align { get; set; } = ContentAlign.Center | ContentAlign.Top;
}
