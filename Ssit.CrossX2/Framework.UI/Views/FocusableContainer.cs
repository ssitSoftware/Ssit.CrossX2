using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class FocusableContainer: Container, IFocusableView
{
    public string UniqueId { get; set; }
    public IValueConsumer<bool> FocusConsumer { get; set; }

    public ColorWrapper? FocusBackgroundColor { get; set; }
    public ColorWrapper? FocusColor { get; set; }
    public ColorWrapper? FocusOutlineColor { get; set; }
}