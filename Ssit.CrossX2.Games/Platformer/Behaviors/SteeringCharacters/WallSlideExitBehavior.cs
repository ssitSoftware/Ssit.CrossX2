using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Ends a wall slide when the character reaches the ground (-> "Run") or when no slidable wall is beside it anymore (-> "Fall").
/// A wall made only of NoSlideMaterials counts as no wall.
/// </summary>
public class WallSlideExitBehavior : SteeringBehavior<ISteeringCharacter>
{
    // Collider materials the character can't slide down
    private readonly HashSet<int> _noSlideMaterials = new();

    // Reused buffer for colliders found beside the character
    private readonly List<ICollider> _collidersBuffer = new();

    public int[] NoSlideMaterials
    {
        set
        {
            foreach (var material in value)
            {
                _noSlideMaterials.Add(material);
            }
        }
    }

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

        _collidersBuffer.Clear();
        obj.Body.Simulation.CheckCollision(wallProbe, obj.Body, obj.Body.Colliders[0].Material.CollidesWith, colliders: _collidersBuffer);

        if (!HasSlidableWall())
        {
            obj.SetSteeringState("Fall");
            return true;
        }

        return false;
    }

    private bool HasSlidableWall()
    {
        for (var idx = 0; idx < _collidersBuffer.Count; ++idx)
        {
            if (!_noSlideMaterials.Contains(_collidersBuffer[idx].Material.Index))
            {
                return true;
            }
        }

        return false;
    }
}
