using Ssit.CrossX2.Framework.Games.Logic.Objects;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

/// <summary>
/// Per-bot targeting state shared by the brain behaviors (and steering behaviors such as horde alerts).
/// </summary>
public class AttackTargetParameters
{
    // Currently chased target, null when there is none
    public IHittable Target;

    /// <summary>Backoffs in a row without an attack - a high count means the target can't be reached.</summary>
    public int BackoffCount;

    /// <summary>Target given up on as unreachable; ignored by scanning/alerts until <see cref="IgnoreTimeRemaining"/> runs out.</summary>
    public IHittable IgnoredTarget;
    // Seconds left until the ignored target may be picked up again
    public float IgnoreTimeRemaining;

    /// <summary>
    /// True if the target was given up on and the ignore time has not run out yet.
    /// </summary>
    public bool IsIgnored(IHittable target) =>
        target is not null && ReferenceEquals(target, IgnoredTarget) && IgnoreTimeRemaining > 0;

    /// <summary>
    /// Starts chasing a new target and resets the backoff counter.
    /// </summary>
    public void SetTarget(IHittable target)
    {
        Target = target;
        BackoffCount = 0;
    }

    /// <summary>
    /// Drops the current target as unreachable and ignores it for ignoreTime seconds.
    /// </summary>
    public void GiveUpTarget(float ignoreTime)
    {
        IgnoredTarget = Target;
        IgnoreTimeRemaining = ignoreTime;
        Target = null;
        BackoffCount = 0;
    }
}
