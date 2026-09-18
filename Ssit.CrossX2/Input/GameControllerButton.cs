namespace Ssit.CrossX2.Input;

/// <summary>
/// Indicates the movement of the right analog stick toward the left direction on a game controller.
/// </summary>
public enum GameControllerButton
{
    None = -1,
    A = 0,
    B = 1,
    X = 2,
    Y = 3,
    Back = 4,
    Guide = 5,
    Start = 6,
    LeftStick = 7,
    RightStick = 8,
    LeftShoulder = 9,
    RightShoulder = 10,
    DPadUp = 11,
    DPadDown = 12,
    DPadLeft = 13,
    DPadRight = 14,
    BasicMax,
    
    /// <summary>
    /// Represents the state when the left stick of a game controller is pushed to the up.
    /// </summary>
    LeftStickUp = BasicMax,

    /// <summary>
    /// Represents the state when the left stick of a game controller is pushed to the down.
    /// </summary>
    LeftStickDown,

    /// <summary>
    /// Represents the state when the left stick of a game controller is pushed to the left.
    /// </summary>
    LeftStickLeft,

    /// <summary>
    /// Represents the state when the left stick of a game controller is pushed to the right.
    /// </summary>
    LeftStickRight,

    /// <summary>
    /// Represents the state when the right stick of a game controller is pushed to the up.
    /// </summary>
    RightStickUp,

    /// <summary>
    /// Represents the state when the right stick of a game controller is pushed to the down.
    /// </summary>
    RightStickDown,

    /// <summary>
    /// Represents the state when the right stick of a game controller is pushed to the left.
    /// </summary>
    RightStickLeft,

    /// <summary>
    /// Represents the state when the right stick of a game controller is pushed to the right.
    /// </summary>
    RightStickRight,

    /// <summary>
    /// Represents the state when the left trigger on a game controller is pushed.
    /// </summary>
    LeftTrigger,

    /// <summary>
    /// Represents the state when the right trigger on a game controller is pushed.
    /// </summary>
    RightTrigger,
    Max
}