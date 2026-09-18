namespace Ssit.CrossX2.Input;

public interface INativeTextInput: IDisposable
{
    bool IsShiftPressed { get; }
    void UpdatePosition(RectangleF bounds, int cursorPosition);
    void Reactivate();
}