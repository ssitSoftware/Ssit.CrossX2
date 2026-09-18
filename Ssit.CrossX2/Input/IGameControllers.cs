namespace Ssit.CrossX2.Input;

/// <summary>
/// Service for game controller input handling.
/// </summary>
public interface IGameControllers
{
    /// <summary>
    /// Toggles the button configuration for a specified game controller player.
    /// </summary>
    /// <param name="player">The player index (e.g., 0 for the first player) whose button configuration is to be switched.</param>
    /// <param name="switch">Indicates whether to enable or disable the button switch mode.</param>
    /// <returns>True if the operation was successful, otherwise false.</returns>
    void SwitchButtons(int player, bool @switch);
    
    /// <summary>
    /// Gets or sets the vibration force for the game controllers.
    /// </summary>
    /// <remarks>
    /// The vibration force is represented as a byte value.
    /// </remarks>
    byte VibrationForce { get; set; }

    /// <summary>
    /// Gets the state of a specified game controller button for a given player.
    /// </summary>
    /// <param name="player">The player index (e.g., 0 for the first player).</param>
    /// <param name="button">The game controller button whose state is to be retrieved.</param>
    /// <returns>The state of the specified game controller button.</returns>
    ButtonState GetButton(int player, GameControllerButton button);

    /// <summary>
    /// Retrieves the value of the specified axis for the given player.
    /// </summary>
    /// <param name="player">The player index to get the axis value for.</param>
    /// <param name="axis">The specific axis to retrieve the value from.</param>
    /// <returns>A float representing the axis value, ranging from -1.0 to 1.0.</returns>
    float GetAxis(int player, GameControllerAxis axis);

    /// <summary>
    /// Determines whether the specified player has a connected game controller.
    /// </summary>
    /// <param name="player">The player index (e.g., 0 for the first player).</param>
    /// <returns>A boolean value indicating whether the player's game controller is connected.</returns>
    bool IsConnected(int player);

    /// <summary>
    /// Triggers the vibration motors of a game controller for a specified duration.
    /// </summary>
    /// <param name="player">The index of the player whose controller should vibrate (e.g., 0 for the first player).</param>
    /// <param name="low">The intensity level for the low-frequency vibration motor.</param>
    /// <param name="high">The intensity level for the high-frequency vibration motor.</param>
    /// <param name="ms">The duration of the vibration in milliseconds.</param>
    void Vibrate(int player, Vibration low, Vibration high, uint ms);
}