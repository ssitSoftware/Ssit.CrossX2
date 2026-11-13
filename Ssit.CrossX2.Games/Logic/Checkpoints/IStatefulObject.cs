namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Map object whose state is stored in checkpoints. Implement <see cref="IStatefulObject{TState}"/> instead of this
/// interface directly. Only objects created from the map (MapId != 0) are saved.
/// </summary>
public interface IStatefulObject
{
    Type StateType { get; }
    
    object CaptureState();
    
    void RestoreState(object state);
}

/// <summary>
/// Map object whose state is stored in checkpoints.
/// </summary>
/// <typeparam name="TState">
/// Plain, JSON-serializable state class (by convention a nested <c>State</c> class of the object).
/// New properties can be added freely - missing values keep their defaults when older checkpoints are loaded.
/// </typeparam>
public interface IStatefulObject<TState> : IStatefulObject where TState : class
{
    Type IStatefulObject.StateType => typeof(TState);
    object IStatefulObject.CaptureState() => CaptureState();
    void IStatefulObject.RestoreState(object state) => RestoreState((TState)state);
    
    /// <summary>
    /// Returns the current state of the object. Return null to skip saving the object.
    /// </summary>
    new TState CaptureState();
    
    /// <summary>
    /// Applies a stored state. Called after the map is loaded and links are resolved, before the first update.
    /// Should set the state silently (no events, sounds or transition animations).
    /// </summary>
    void RestoreState(TState state);
}

/// <summary>
/// Called on map objects after all of them were restored from a checkpoint, so that objects can re-sync with
/// the objects they are linked to.
/// </summary>
public interface IStateRestoredHandler
{
    void OnStateRestored();
}
