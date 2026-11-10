using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>
/// Per-object state shared by flying enemy behaviors (accessed via GetParameters).
/// </summary>
public class FlyingEnemyParameters
{
    /// <summary>Currently tracked target (usually the player). Null when there is none.</summary>
    public IHittable Target;

    /// <summary>Position the enemy was placed at (perch). Used when returning.</summary>
    public Vector2 HomePosition;

    /// <summary>Direction locked at the start of a dive.</summary>
    public Vector2 AttackDirection;

    /// <summary>Time spent close to the hover point above the target.</summary>
    public float HoverTime;

    /// <summary>Time since the current timed state (e.g. dive) started.</summary>
    public float StateTime;

    /// <summary>Total flight time - drives the side-to-side sway above the target.</summary>
    public float FlightTime;
}
