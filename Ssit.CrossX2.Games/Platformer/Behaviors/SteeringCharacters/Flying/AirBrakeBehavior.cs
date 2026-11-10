using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>Slows a gravity-less body down to a stop (air drag).</summary>
public class AirBrakeBehavior(float deceleration) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        obj.Body.Velocity = FlightSteering.MoveTowards(obj.Body.Velocity, Vector2.Zero, deceleration * dt);
        return false;
    }
}
