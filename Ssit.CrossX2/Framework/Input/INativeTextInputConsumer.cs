namespace Ssit.CrossX2.Framework.Input;

public interface INativeTextInputConsumer
{
    void OnTextInput(string text);
    void OnTextInputClosed();
    bool OnKey(Key key);
}