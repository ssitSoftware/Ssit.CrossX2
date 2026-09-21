using Ssit.CrossX2.Framework.Input;

#if IOS
namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;

internal class NativeTextInputIos(NativeTextInputServiceIos service) : INativeTextInput
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

    public void Reactivate() { }
}
#endif
