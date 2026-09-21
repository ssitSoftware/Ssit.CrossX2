namespace Ssit.CrossX2.Framework.Input;

/// <summary>
/// Provides methods to query the state of keys on a keyboard.
/// </summary>
public interface IKeyboard
{
    /// <summary>
    /// Retrieves the current state of the specified key.
    /// </summary>
    /// <param name="key">The key whose state is to be retrieved.</param>
    /// <returns>A <see cref="ButtonState"/> indicating the current state of the key.</returns>
    ButtonState GetKey(Key key);
}