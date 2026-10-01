using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public interface IFocusableView
{
    public string UniqueId { get; set; }
    public IValueConsumer<bool> FocusConsumer { get; set; }
}