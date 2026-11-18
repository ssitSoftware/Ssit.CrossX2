namespace Ssit.CrossX2.Framework.Games.Logic;

public interface IGameState
{
    event Action StateUpdated;

    /// <summary>
    /// Raised (scheduled, like <see cref="StateUpdated"/>) when <see cref="SetFlags"/> sets flags that weren't set yet,
    /// with those flags. Not raised by <see cref="RestoreFlags"/>.
    /// </summary>
    event Action<IReadOnlyList<string>> FlagsSet;
    bool HasFlag(string flag);
    void SetFlags(string flag);
    
    /// <summary>
    /// All flags currently set. Used to store the game state in a checkpoint.
    /// </summary>
    IReadOnlyCollection<string> Flags { get; }
    
    /// <summary>
    /// Replaces all flags with the given ones. Used when the game state is restored from a checkpoint.
    /// </summary>
    void RestoreFlags(IEnumerable<string> flags);
}