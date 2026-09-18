namespace Ssit.CrossX2.Input;

public interface INativeTextInputService
{
    INativeTextInput AllocateTextInput(INativeTextInputConsumer consumer, InputType inputType);
}