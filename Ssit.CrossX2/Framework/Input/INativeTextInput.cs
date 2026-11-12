namespace Ssit.CrossX2.Framework.Input;

/// <summary>
/// Represents an active native text input session.
/// </summary>
public interface INativeTextInput: IDisposable
{
    bool IsShiftPressed { get; }
    void UpdatePosition(RectangleF bounds, int cursorPosition);
    void Reactivate();
}
