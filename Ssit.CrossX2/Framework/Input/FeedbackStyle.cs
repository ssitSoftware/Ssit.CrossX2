namespace Ssit.CrossX2.Framework.Input;

/// <summary>
/// Identifies the kind of haptic feedback to play.
/// </summary>
public enum FeedbackStyle
{
    ButtonRelease,
    ButtonPush,
    ControlChangeValue,
    RumbleLight,
    RumbleMedium,
    RumbleHeavy,
}

/// <summary>
/// Configures how much haptic feedback is allowed.
/// </summary>
public enum FeedbackLevel
{
    Disabled = 0,
    Level1 = 1,
    Level2 = 2,
    Level3 = 3
}
