using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Parameters;

/// <summary>
/// Per-character data about the last landing, written by <see cref="LandingTransitionBehavior"/>
/// and used by <see cref="ApplyHorizontalFrictionBehavior"/> to modify friction right after landing.
/// </summary>
public sealed class LandingParameters
{
    // Velocity at the moment of landing; cleared once the landing slide is over
    public Vector2? Velocity { get; set; }
}
