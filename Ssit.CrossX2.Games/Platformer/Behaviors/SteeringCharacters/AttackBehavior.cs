using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Starts a ground attack when the given control is pressed: lunges forward with AttackVelocity and switches to attackStateName.
/// Presses are ignored while the post-attack cooldown (<see cref="AttackingData.NextAttackTimeout"/>) is running.
/// </summary>
public class AttackBehavior(string control, string attackStateName) : SteeringBehavior<ISteeringCharacter>
{
    /// <summary>
    /// Per-character attack state.
    /// </summary>
    public class AttackingData
    {
        // Remaining cooldown in seconds before another attack may start
        public float NextAttackTimeout;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<AttackingData>();
        if (parameters.NextAttackTimeout > 0)
        {
            parameters.NextAttackTimeout -= dt;
            return false;
        }
        
        if (obj.SteeringInput.Button(control) != ButtonState.JustPressed)
            return false;

        obj.Body.Velocity = obj.Body.Velocity
            with { X = obj.FaceLeft ? 
                -obj.PhysicsValues.AttackVelocity : obj.PhysicsValues.AttackVelocity };
        
        obj.SetSteeringState(attackStateName);
        return true;
    }
}
