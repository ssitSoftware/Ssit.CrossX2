namespace Ssit.CrossX2.Input;

public interface INativeTextInputConsumer
{
    void OnTextInput(string text);
    void OnTextInputClosed();
    bool OnKey(Key key);
}