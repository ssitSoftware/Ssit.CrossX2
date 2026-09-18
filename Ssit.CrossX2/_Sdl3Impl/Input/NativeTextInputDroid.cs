#if ANDROID
using Ssit.CrossX2.Input;

namespace Ssit.CrossX2._Sdl3Impl.Input;

internal class NativeTextInputDroid(NativeTextInputServiceDroid service) : INativeTextInput
{
    private bool _disposed;

    public bool IsShiftPressed => service.IsShiftPressed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        service.OnDisposed(this);
    }

    public void UpdatePosition(RectangleF bounds, int cursorPosition)
    {
        if (!_disposed)
        {
            service.UpdatePosition(bounds, cursorPosition);
        }
    }

    public void Reactivate()
    {
        if (!_disposed)
        {
            service.Reactivate(this);
        }
    }
}
#endif
