using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class WallSlideTimeoutBehavior : SteeringBehavior<ISteeringCharacter>
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class Parameters
    {
        public float Elapsed;
    }

    protected override void OnEnter(ISteeringCharacter obj)
    {
        obj.GetParameters<Parameters>(true).Elapsed = 0f;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<Parameters>(true);
        parameters.Elapsed += dt;
        if (parameters.Elapsed < obj.PhysicsValues.WallSlideTimeout)
            return false;

        obj.Body.Velocity = obj.Body.Velocity with { X = 0 };
        obj.Body.Position += obj.FaceLeft ? new Vector2(0.1f, 0.0f) : new Vector2(-0.1f, 0.0f);

        obj.SetSteeringState("Fall");
        return true;
    }
}
