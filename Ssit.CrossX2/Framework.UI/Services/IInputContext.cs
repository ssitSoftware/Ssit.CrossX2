using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Services;

public interface IInputContext
{
    void CapturePointer(int pointerId, IInputConsumer captureBy);
    bool Focus(IFocusable focusable, object caller);
    IFocusable FindFocusable(string uniqueId, object caller);
    bool MoveFocus(FocusDirection direction, object caller);
    bool IsUiButtonDown(UiButton button);
}