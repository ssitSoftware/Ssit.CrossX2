using System.Numerics;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>
/// Looks for an enemy (player) in a radius around the object (both sides). When found, stores it as the target,
/// turns towards it and switches to <paramref name="nextState"/>.
/// <see cref="Range"/> can be changed later (e.g. when an editor link resolves).
/// With <see cref="NearHome"/> the last lost target is also reacquired when it is within the range of the home position
/// (the object may be far from home - e.g. returning or stuck). Nothing is acquired during
/// <see cref="FlyingEnemyParameters.ReacquireCooldown"/>.
/// </summary>
public class DetectTargetInRangeBehavior(float range, string nextState) : SteeringBehavior<ISteeringCharacter>
{
    private float _range = range;
    private BoxHitDetector _detector = CreateDetector(range);

    public float Range
    {
        get => _range;
        set
        {
            _range = value;
            _detector = CreateDetector(value);
        }
    }

    /// <summary>Also reacquire the last lost target when it comes within the range of the home position.</summary>
    public bool NearHome { get; init; }

    // Square box covering the range on both sides; the exact circular range is checked afterwards
    private static BoxHitDetector CreateDetector(float range) => new(new Vector2(0, -0.5f), new SizeF(range * 2, range * 2));

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.FlyingParameters();

        if (parameters.ReacquireCooldown > 0)
        {
            parameters.ReacquireCooldown -= dt;
            return false;
        }

        var target = DetectAround(obj) ?? DetectNearHome(parameters);

        if (target is null)
            return false;

        parameters.Target = target;
        obj.FaceLeft = target.Position.X < obj.Body.Position.X;
        obj.SetSteeringState(nextState);
        return true;
    }

    private IHittable DetectAround(ISteeringCharacter obj)
    {
        var result = _detector.Detect(obj.Body, obj.FaceLeft, IMaterial.AllColliders, FlightSteering.IsEnemyOf, ColliderSides.None);

        if (result.Hittable is not { Alive: true } target)
            return null;

        return Vector2.Distance(target.Position, obj.Body.Position) > _range ? null : target;
    }

    private IHittable DetectNearHome(FlyingEnemyParameters parameters)
    {
        if (!NearHome || parameters.LastTarget is not { Alive: true } target)
            return null;

        return Vector2.Distance(target.Position, parameters.HomePosition) > _range ? null : target;
    }
}
