using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public abstract class ChildrenContainer : Background
{
    public IList<View> Children { get; set; } = [];
    public SignalSource<View> LayoutSignal { get; set; }
    public Thickness? Padding { get; set; }
}