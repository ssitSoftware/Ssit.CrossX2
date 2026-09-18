using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Handlers;

public interface IFocusable
{
    bool Enabled { get; }
    bool Focused { get; }
    RectangleF ScreenBounds { get; }
    bool DisableAllInput { get; }
    bool OnUiButton(UiButton button, IInputContext inputContext);
    void SetFocus();
    bool ResetFocus();
    string UniqueId { get; }
    bool SkipNavigation { get; }
}