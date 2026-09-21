using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class WallSlideExitBehavior : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringParameters.IsOnGround)
        {
            obj.SetSteeringState("Run");
            return true;
        }
        
        var aabb = obj.Body.Colliders[0].Aabb;
        var wallProbe = obj.FaceLeft
            ? new Aabb(aabb.Left - 0.1f, aabb.Top + 0.1f, aabb.Left, aabb.Bottom - 0.1f)
            : new Aabb(aabb.Right, aabb.Top + 0.1f, aabb.Right + 0.1f, aabb.Bottom - 0.1f);

        if (!obj.Body.Simulation.CheckCollision(wallProbe, obj.Body, obj.Body.Colliders[0].Material.ColliderGroup))
        {
            obj.SetSteeringState("Fall");
            return true;
        }

        return false;
    }
}
