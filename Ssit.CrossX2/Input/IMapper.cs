namespace Ssit.CrossX2.Input;

/// <summary>
/// Defines methods for mapping game controller axes and buttons,
/// as well as keyboard keys, to specific actions or inputs.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Maps a game controller axis to a specified axis name.
    /// </summary>
    /// <param name="axisName">The name of the axis to map.</param>
    /// <param name="axis">The <see cref="GameControllerAxis"/> to map to the axis name.</param>
    /// <returns>The <see cref="IMapper"/> instance for chaining additional mappings.</returns>
    public IMapper MapAxis(string axisName, GameControllerAxis axis);

    /// <summary>
    /// Maps a pair of game controller buttons to an axis, allowing the axis to be controlled by the negative and positive buttons.
    /// </summary>
    /// <param name="axisName">The name of the axis to map.</param>
    /// <param name="negative">The button that represents the negative direction of the axis.</param>
    /// <param name="positive">The button that represents the positive direction of the axis.</param>
    /// <returns>An IMapper instance to chain further mapping calls.</returns>
    public IMapper MapAxis(string axisName, GameControllerButton negative, GameControllerButton positive);

    /// <summary>
    /// Maps an axis to a pair of keyboard keys to act as the negative and positive values of the axis.
    /// </summary>
    /// <param name="axisName">The name of the axis to map.</param>
    /// <param name="negative">The key representing the negative direction of the axis.</param>
    /// <param name="positive">The key representing the positive direction of the axis.</param>
    /// <returns>An instance of the <see cref="IMapper"/> interface for method chaining.</returns>
    public IMapper MapAxis(string axisName, Key negative, Key positive);

    /// <summary>
    /// Maps a game controller button to a given button name.
    /// </summary>
    /// <param name="buttonName">The name to map to the game controller button.</param>
    /// <param name="button">The game controller button to map.</param>
    /// <param name="alt">Alternative game controller button to map.</param>
    /// <return>An instance of IMapper to allow for method chaining.</return>
    public IMapper MapButton(string buttonName, GameControllerButton button, GameControllerButton alt = GameControllerButton.None);

    /// <summary>
    /// Maps a specified key to a button action on the input controller.
    /// </summary>
    /// <param name="buttonName">The name of the button action to map.</param>
    /// <param name="key">The key to map to the button action.</param>
    /// <param name="alt">Alternative key to map to the button action.</param>
    /// <returns>The current instance of <see cref="IMapper"/> to allow for method chaining.</returns>
    public IMapper MapButton(string buttonName, Key key, Key alt = Key.None);

    /// <summary>
    /// Clears all the current input mappings, including axis and button mappings.
    /// </summary>
    /// <returns>
    /// The instance of IMapper for chaining further method calls.
    /// </returns>
    public IMapper Clear();
}