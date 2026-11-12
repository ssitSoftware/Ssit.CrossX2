using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Stering;

/// <summary>
/// Base class for a reusable piece of logic plugged into a steering state (see <see cref="SteeringStateWithBehaviors{TObject}"/>).
/// A state runs its behaviors in order for every callback. Callbacks returning <c>bool</c> return true when they handled the
/// callback (usually by changing the steering state) - the remaining behaviors of the state are then skipped for that callback.
/// Behaviors are shared between objects, so per-object data belongs in <c>ICharacter.GetParameters&lt;T&gt;()</c>, not in fields.
/// </summary>
public class SteeringBehavior<TObject>
{
    // Entry points called by the steering state; they forward to the overridable On* methods
    internal void Enter(TObject obj) => OnEnter(obj);
    internal void Exit(TObject obj) => OnExit(obj);
    internal bool Event(TObject obj, ISpriteEvent @event) => OnEvent(obj, @event);
    internal bool SequenceFinished(TObject obj, string name) => OnSequenceFinished(obj, name);
    internal bool Collision(TObject obj, ICollider source, ICollider other, Vector2 impact) => OnCollision(obj, source, other, impact);   
    internal bool Update(TObject obj, float dt) => OnUpdate(obj, dt);
    internal bool FixedUpdate(TObject obj, float dt) => OnFixedUpdate(obj, dt);
    
    /// <summary>
    /// Called every physics step while the state is active. Return true to stop the following behaviors.
    /// </summary>
    protected virtual bool OnFixedUpdate(TObject obj, float dt) => false;
    /// <summary>
    /// Called every frame while the state is active. Return true to stop the following behaviors.
    /// </summary>
    protected virtual bool OnUpdate(TObject obj, float dt) => false;
    
    /// <summary>
    /// Called for sprite animation events (named frames such as "Slash" or "Step"). Return true to stop the following behaviors.
    /// </summary>
    protected virtual bool OnEvent(TObject obj, ISpriteEvent @event) => false;
    
    /// <summary>
    /// Called when the current sprite sequence (animation) ends. Return true to stop the following behaviors.
    /// </summary>
    protected virtual bool OnSequenceFinished(TObject obj, string name) => false;
    
    /// <summary>
    /// Called when the owning state becomes active.
    /// </summary>
    protected virtual void OnEnter(TObject character)
    {
    }

    /// <summary>
    /// Called when the owning state is left.
    /// </summary>
    protected virtual void OnExit(TObject obj)
    {
    }

    /// <summary>
    /// Called when one of the object's colliders (source) hits another collider; impact is the collision response vector.
    /// Return true to stop the following behaviors.
    /// </summary>
    protected virtual bool OnCollision(TObject obj, ICollider source, ICollider other, Vector2 impact) => false;
}