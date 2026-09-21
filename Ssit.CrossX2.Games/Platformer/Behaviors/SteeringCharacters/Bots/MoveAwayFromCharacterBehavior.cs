using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

public class MoveAwayFromCharacterBehavior(string afterState) : SteeringBehavior<ISteeringCharacter>
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class Parameters
    {
        public float StartX;
        public float MoveDirection;
    }

    protected override bool OnCollision(ISteeringCharacter obj, ICollider source, ICollider other, Vector2 impact)
    {
        if (other.AttachedBody?.Owner is not ICharacter)
            return false;

        var otherCenterX = other.Aabb.Center.X;
        var ourCenterX = source.Aabb.Center.X;

        var parameters = obj.GetParameters<Parameters>(true);
        parameters.MoveDirection = otherCenterX < ourCenterX ? 1f : -1f;
        obj.FaceLeft = parameters.MoveDirection > 0f;

        parameters.StartX = obj.Body.Position.X;
        obj.SetSteeringState("MoveAway");
        return true;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<Parameters>(true);
        if (parameters.MoveDirection == 0f)
            return false;

        if (MathF.Abs(obj.Body.Position.X - parameters.StartX) >= obj.PhysicsValues.MoveAwayDistance)
        {
            parameters.MoveDirection = 0f;
            obj.SetSteeringState(afterState);
            return true;
        }

        obj.Body.Velocity = obj.Body.Velocity with { X = parameters.MoveDirection * obj.PhysicsValues.MoveAwaySpeed };
        return false;
    }
}
