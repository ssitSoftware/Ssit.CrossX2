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

    /// <summary>Last target that was lost - can be reacquired when it comes back near home.</summary>
    public IHittable LastTarget;

    /// <summary>Time left during which no target is (re)acquired - set after giving up on an unreachable target.</summary>
    public float ReacquireCooldown;

    /// <summary>Stuck check (see FlightSteering.UpdateStuck): position in the previous update.</summary>
    public Vector2 StuckLastPosition;

    /// <summary>Stuck check: distance moved towards the goal in the current check window.</summary>
    public float StuckProgress;

    /// <summary>Stuck check: time since the current check window started.</summary>
    public float StuckTime;

    /// <summary>Time left of the current detour around an obstacle (returning home).</summary>
    public float DetourTime;

    /// <summary>Direction of the current detour.</summary>
    public Vector2 DetourDirection;

    /// <summary>Detours tried since the return started - alternates the detour side and makes them longer.</summary>
    public int DetourCount;
}
