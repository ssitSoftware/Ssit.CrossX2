using Ssit.CrossX2.Framework.Games.Logic.Objects;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

public class AttackTargetParameters
{
    public IHittable Target;

    /// <summary>Backoffs in a row without an attack - a high count means the target can't be reached.</summary>
    public int BackoffCount;

    /// <summary>Target given up on as unreachable; ignored by scanning/alerts until <see cref="IgnoreTimeRemaining"/> runs out.</summary>
    public IHittable IgnoredTarget;
    public float IgnoreTimeRemaining;

    public bool IsIgnored(IHittable target) =>
        target is not null && ReferenceEquals(target, IgnoredTarget) && IgnoreTimeRemaining > 0;

    public void SetTarget(IHittable target)
    {
        Target = target;
        BackoffCount = 0;
    }

    public void GiveUpTarget(float ignoreTime)
    {
        IgnoredTarget = Target;
        IgnoreTimeRemaining = ignoreTime;
        Target = null;
        BackoffCount = 0;
    }
}
