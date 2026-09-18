namespace Ssit.CrossX2.Input.Internal;

public abstract class KeyboardBase: IKeyboard
{
    public ButtonState GetKey(Key key)
    {
        if(key == Key.None)
            return ButtonState.Empty;
        
        return GetKeyInternal(key);
    }
    protected abstract ButtonState GetKeyInternal(Key key);
}