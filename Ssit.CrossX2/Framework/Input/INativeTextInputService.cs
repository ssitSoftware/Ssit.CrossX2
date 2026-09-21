namespace Ssit.CrossX2.Framework.Input;

public interface INativeTextInputService
{
    INativeTextInput AllocateTextInput(INativeTextInputConsumer consumer, InputType inputType);
}