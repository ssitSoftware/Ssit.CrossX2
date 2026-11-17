namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Player whose state is stored in the game state of the slot (<see cref="GameStateData.PlayerState"/>), not in the
/// map state - it goes with the player from map to map. Implement <see cref="IPlayerStateProvider{TState}"/> instead
/// of this interface directly. Only one object in the simulation should implement it.
/// </summary>
public interface IPlayerStateProvider
{
    Type PlayerStateType { get; }
    
    object CapturePlayerState();
    
    void RestorePlayerState(object state);
}

/// <summary>
/// Player whose state is stored in the game state of the slot. The state is captured by the player itself and passed
/// to <see cref="ICheckpointService.Save"/>; it is restored by <see cref="ICheckpointService.Restore"/>
/// after the map objects and before <see cref="IStateRestoredHandler.OnStateRestored"/>.
/// </summary>
/// <typeparam name="TState">
/// Plain, JSON-serializable state class.
/// New properties can be added freely - missing values keep their defaults when older checkpoints are loaded.
/// </typeparam>
public interface IPlayerStateProvider<TState> : IPlayerStateProvider where TState : class
{
    Type IPlayerStateProvider.PlayerStateType => typeof(TState);
    object IPlayerStateProvider.CapturePlayerState() => CapturePlayerState();
    void IPlayerStateProvider.RestorePlayerState(object state) => RestorePlayerState((TState)state);
    
    /// <summary>
    /// Returns the current state of the player.
    /// </summary>
    new TState CapturePlayerState();
    
    /// <summary>
    /// Applies a stored state. Called after the map objects were restored, before
    /// <see cref="IStateRestoredHandler.OnStateRestored"/>. Should set the state silently (no events, sounds or animations).
    /// </summary>
    void RestorePlayerState(TState state);
}
