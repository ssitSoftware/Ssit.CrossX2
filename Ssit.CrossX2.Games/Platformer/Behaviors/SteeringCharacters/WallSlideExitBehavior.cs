using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Ends a wall slide when the character reaches the ground (-> "Run") or when no wall is beside it anymore (-> "Fall").
/// </summary>
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
        // Thin box beside the character on the wall side, inset vertically to ignore floor and ceiling
        var wallProbe = obj.FaceLeft
            ? new Aabb(aabb.Left - 0.1f, aabb.Top + 0.1f, aabb.Left, aabb.Bottom - 0.1f)
            : new Aabb(aabb.Right, aabb.Top + 0.1f, aabb.Right + 0.1f, aabb.Bottom - 0.1f);

        if (!obj.Body.Simulation.CheckCollision(wallProbe, obj.Body, obj.Body.Colliders[0].Material.CollidesWith))
        {
            obj.SetSteeringState("Fall");
            return true;
        }

        return false;
    }
}
