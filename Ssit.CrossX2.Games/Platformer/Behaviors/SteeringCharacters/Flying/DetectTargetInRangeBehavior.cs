using System.Numerics;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>
/// Looks for an enemy (player) in a radius around the object (both sides). When found, stores it as the target,
/// turns towards it and switches to <paramref name="nextState"/>.
/// </summary>
public class DetectTargetInRangeBehavior(float range, string nextState) : SteeringBehavior<ISteeringCharacter>
{
    private readonly BoxHitDetector _detector = new(new Vector2(0, -0.5f), new SizeF(range * 2, range * 2));

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var result = _detector.Detect(obj.Body, obj.FaceLeft, IMaterial.AllColliders, FlightSteering.IsEnemyOf, ColliderSides.None);

        if (result.Hittable is not { Alive: true } target)
            return false;

        if (Vector2.Distance(target.Position, obj.Body.Position) > range)
            return false;

        obj.FlyingParameters().Target = target;
        obj.FaceLeft = target.Position.X < obj.Body.Position.X;
        obj.SetSteeringState(nextState);
        return true;
    }
}
