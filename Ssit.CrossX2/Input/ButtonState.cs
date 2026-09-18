namespace Ssit.CrossX2.Input;

/// <summary>
/// Represents the state of a button, indicating whether it is pressed or not
/// and whether its state has changed.
/// </summary>
public readonly struct ButtonState : IEquatable<ButtonState>
{
    public bool Equals(ButtonState other)
    {
        return IsDown == other.IsDown && IsChanged == other.IsChanged;
    }

    public override bool Equals(object obj)
    {
        return obj is ButtonState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsDown, IsChanged);
    }

    /// <summary>
    /// Represents default button state (unpressed and not changed in current pass).
    /// </summary>
    public static readonly ButtonState Empty = new ();

    /// <summary>
    /// Represents a state where the button is already pressed.
    /// This state indicates that the button is pressed down
    /// and that there hasn't been a state change.
    /// </summary>
    public static readonly ButtonState Down = new (true, false);
    
    /// <summary>
    /// Represents a state where the button has just been pressed.
    /// This state indicates that the button is currently pressed down
    /// and that there has been a state change.
    /// </summary>
    public static readonly ButtonState JustPressed = new (true, true);

    /// <summary>
    /// Represents a button state where the button has just been released.
    /// The button is not pressed down and its state has changed.
    /// </summary>
    public static readonly ButtonState JustReleased = new (false, true);

    public static ButtonState FromStates(bool isDown, bool wasDown) => new ButtonState(isDown, isDown != wasDown);

    /// <summary>
    /// Defines a custom bitwise OR operation for combining two <see cref="ButtonState"/> instances.
    /// The resulting state will indicate whether either of the input states
    /// represents a button being pressed and whether a state change has occurred.
    /// </summary>
    public static ButtonState operator |(ButtonState left, ButtonState right)
    {
        var isDown  = left.IsDown || right.IsDown;
        var wasDown1 = left is { IsDown: true, IsChanged: false };
        var wasDown2 = right is { IsDown: true, IsChanged: false };
        
        var wasDown = wasDown1 || wasDown2;
        
        return new ButtonState(isDown, isDown != wasDown);
    }
    
    /// <summary>
    /// Defines a custom bitwise OR operation for combining two <see cref="ButtonState"/> instances.
    /// The resulting state will indicate whether either of the input states
    /// represents a button being pressed and whether a state change has occurred.
    /// </summary>
    public ButtonState WithModifier(ButtonState modifierState)
    {
        var state = this & modifierState;
        
        if (state == JustPressed && !IsChanged)
        {
            state = Down;
        }

        return state;
    }

    /// <summary>
    /// Performs a bitwise AND operation on two <see cref="ButtonState"/> instances.
    /// The resulting state indicates that the button is pressed only if both input states
    /// indicate the button is pressed. Additionally, the resulting state will reflect
    /// if either input state has registered a state change.
    /// </summary>
    public static ButtonState operator &(ButtonState left, ButtonState right)
    {
        var isDown  = left.IsDown && right.IsDown;
        var isChanged = left.IsChanged || right.IsChanged;
        return new ButtonState(isDown, isChanged);
    }
    
    /// <summary>
    /// Represents the state of a button, indicating whether it is pressed or not
    /// and whether its state has changed.
    /// </summary>
    public ButtonState(bool isDown, bool isChanged)
    {
        IsDown = isDown;
        IsChanged = isChanged;
    }

    /// <summary>
    /// Indicates whether the button is currently pressed.
    /// </summary>
    public bool IsDown { get; }

    /// <summary>
    /// Indicates whether the button state has changed in the current pass.
    /// </summary>
    public bool IsChanged { get; }

    public bool WasDown => IsDown ^ IsChanged;
    
    public static bool operator == (ButtonState b1, ButtonState b2)
    {
        return b1.IsChanged == b2.IsChanged && b1.IsDown == b2.IsDown;
    }

    public static bool operator !=(ButtonState b1, ButtonState b2)
    {
        return b1.IsChanged != b2.IsChanged || b1.IsDown != b2.IsDown;
    }
}