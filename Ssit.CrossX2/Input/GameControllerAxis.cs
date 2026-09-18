namespace Ssit.CrossX2.Input;

/// <summary>
/// Defines the various axes available on a game controller.
/// </summary>
public enum GameControllerAxis
{
    /// <summary>
    /// Represents the horizontal axis of the left analog stick on a game controller.
    /// </summary>
    LeftX = 0,

    /// <summary>
    /// Represents the vertical axis of the left analog stick on a game controller.
    /// </summary>
    LeftY = 1,

    /// <summary>
    /// Represents the horizontal axis of the right analog stick on a game controller.
    /// </summary>
    RightX = 2,

    /// <summary>
    /// Represents the vertical of the right analog stick on a game controller.
    /// </summary>
    RightY = 3,

    /// <summary>
    /// Represents the axis corresponding to the left trigger of a game controller.
    /// This axis is used to measure the analog input of the left trigger,
    /// providing a value that ranges from 0 (not pressed) to 1 (fully pressed).
    /// </summary>
    LeftTrigger = 4,

    /// <summary>
    /// Represents the right trigger axis on a game controller.
    /// This axis is used to measure the analog input of the right trigger,
    /// providing a value that ranges from 0 (not pressed) to 1 (fully pressed).
    /// </summary>
    RightTrigger = 5,

    /// <summary>
    /// Represents the horizontal axis of the D-Pad on a game controller.
    /// </summary>
    DPadX = 6,

    /// <summary>
    /// Represents the vertical axis of the D-Pad on a game controller.
    /// </summary>
    DPadY = 7
}