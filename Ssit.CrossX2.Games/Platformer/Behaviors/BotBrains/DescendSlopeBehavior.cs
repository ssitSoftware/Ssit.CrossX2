using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

public class DescendSlopeBehavior(float probeDepth) : Behavior<IBrainCharacter>
{
    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        var body = obj.Body;
        var aabb = body.Colliders[0].Aabb;

        var leftGroundTop = FindGroundTop(body, aabb.Left - 0.1f, aabb.Bottom, probeDepth);
        var rightGroundTop = FindGroundTop(body, aabb.Right + 0.1f, aabb.Bottom, probeDepth);

        if (leftGroundTop is not { } left || rightGroundTop is not { } right)
            return false;

        if (MathF.Abs(left - right) < float.Epsilon)
            return false;

        // Lower ground has a bigger Top value (Y grows downward) - head toward it.
        var faceLeft = left > right;
        obj.FaceLeft = faceLeft;
        obj.InputController.SetValue(SteeringControlNames.HorizontalMove, faceLeft ? -1f : 1f);
        return true;
    }

    private static float? FindGroundTop(IBody body, float x, float fromY, float probeDepth)
    {
        var probe = new Aabb(x - 0.05f, fromY - float.Epsilon, x + 0.05f, fromY + probeDepth);

        float? top = null;
        foreach (var collider in body.Simulation.GetColliders(probe, body, IMaterial.AllColliders, colliderType: ColliderType.Static))
        {
            if (top is not { } current || collider.Aabb.Top < current)
                top = collider.Aabb.Top;
        }

        return top;
    }
}
