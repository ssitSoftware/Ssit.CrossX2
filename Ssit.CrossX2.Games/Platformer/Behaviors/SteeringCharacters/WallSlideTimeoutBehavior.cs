using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Limits how long the character can cling to a wall: after WallSlideTimeout seconds it lets go and falls.
/// The timer is reset while the WallSlide button is held or, with keepSlidingOnPush, while pushing toward the wall.
/// </summary>
public class WallSlideTimeoutBehavior(bool keepSlidingOnPush = false) : SteeringBehavior<ISteeringCharacter>
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class Parameters
    {
        // Seconds spent sliding since the timer was last reset
        public float Elapsed;
    }

    protected override void OnEnter(ISteeringCharacter obj)
    {
        obj.GetParameters<Parameters>().Elapsed = 0f;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<Parameters>();
        parameters.Elapsed += dt;
        
        if (keepSlidingOnPush)
        {
            var move = obj.SteeringInput.Value(SteeringControlNames.HorizontalMove);

            // Pushing toward the wall (the character faces it)
            var shouldReset = obj.FaceLeft ? move < -0.75f : move > 0.75f;
            if (shouldReset)
            {
                parameters.Elapsed = 0;
                return false;
            }
        }

        if (obj.SteeringInput.Button(SteeringControlNames.WallSlide).IsDown)
        {
            parameters.Elapsed = 0;
            return false;
        }
        
        if (parameters.Elapsed < obj.PhysicsValues.WallSlideTimeout)
        {
            return false;
        }

        obj.Body.Velocity = obj.Body.Velocity with { X = 0 };
        // Nudge away from the wall so the fall does not immediately re-trigger wall contact
        obj.Body.Position += obj.FaceLeft ? new Vector2(0.1f, 0.0f) : new Vector2(-0.1f, 0.0f);

        obj.SetSteeringState("Fall");
        return true;
    }
}
