using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Handlers;

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