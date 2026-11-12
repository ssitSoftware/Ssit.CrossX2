namespace Ssit.CrossX2.Framework.Games.Logic;

/// <summary>
/// Base class for a piece of AI logic plugged into a state of a (brain) state machine.
/// The state runs its behaviors in order every fixed update; a behavior returning true from <see cref="OnUpdate"/>
/// stops the remaining ones for that step (usually because it changed the state).
/// Brain behaviors drive the character by writing into its input controller, just like a player's controller would.
/// Behaviors are shared between objects, so per-object data belongs in GetParameters, not in fields.
/// </summary>
public abstract class Behavior<TObject>
{
    // Entry points called by the state; they forward to the overridable On* methods
    internal bool FixedUpdate(TObject obj, float dt) => OnUpdate(obj, dt);
    internal void EnterState(TObject obj) => OnEnterState(obj);
    internal void LeaveState(TObject obj) => OnLeaveState(obj);

    /// <summary>
    /// Called when the owning state is left.
    /// </summary>
    protected virtual void OnLeaveState(TObject obj)
    {
    }

    /// <summary>
    /// Called when the owning state becomes active.
    /// </summary>
    protected virtual void OnEnterState(TObject obj)
    {
    }

    /// <summary>
    /// Called every fixed update while the state is active. Return true to stop the following behaviors.
    /// </summary>
    protected virtual bool OnUpdate(TObject obj, float dt) => false;
}
