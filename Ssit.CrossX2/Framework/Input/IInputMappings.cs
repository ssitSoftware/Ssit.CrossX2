namespace Ssit.CrossX2.Framework.Input;

/// <summary>
/// Service managing input mappings for multiple players.
/// </summary>
public interface IInputMappings
{
    /// <summary>
    /// Retrieves the IMapper instance for a specified player.
    /// Used to define input mappings for a player.
    /// </summary>
    /// <param name="player">The player index for which to get the IMapper instance.</param>
    /// <returns>Returns an IMapper instance for the specified player.</returns>
    IMapper Mapper(int player);

    /// <summary>
    /// Gets input mapping for a specified player.
    /// </summary>
    IInputMapping this[int player] { get; }
}