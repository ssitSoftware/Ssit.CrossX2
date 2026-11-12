namespace Ssit.CrossX2.Framework.Input;

/// <summary>
/// Receives text and key events from a native text input session.
/// </summary>
public interface INativeTextInputConsumer
{
    void OnTextInput(string text);
    void OnTextInputClosed();
    bool OnKey(Key key);
}
