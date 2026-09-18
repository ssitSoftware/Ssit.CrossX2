using Ssit.CrossX2.UI.Parameters;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Views;

public class ScrollView: Background, IFocusableView
{
    public View ContentView { get; set; }
    public View FocusFrameView { get; set; }
    public View ActiveFrameView { get; set; }
    public ScrollMode ScrollMode { get; set; }
    public Length? ScrollExceed { get; set; }
    public Length? AutoScrollSpeedX { get; set; }
    public Length? AutoScrollSpeedY { get; set; }
    public Length? ManualScrollSpeed { get; set; }
    public float AutoScrollResumeDelay { get; set; } = 1f;
    
    public string UniqueId { get; set; }
}