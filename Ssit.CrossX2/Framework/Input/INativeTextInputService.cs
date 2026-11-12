namespace Ssit.CrossX2.Framework.Input;

/// <summary>
/// Allocates platform-native text input sessions for text input consumers.
/// </summary>
public interface INativeTextInputService
{
    INativeTextInput AllocateTextInput(INativeTextInputConsumer consumer, InputType inputType);
}
