using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

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