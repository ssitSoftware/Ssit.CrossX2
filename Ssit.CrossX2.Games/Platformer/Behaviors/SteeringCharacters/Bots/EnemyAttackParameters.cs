namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

/// <summary>
/// Per-enemy attack bookkeeping shared by the bot attack behaviors.
/// </summary>
public class EnemyAttackParameters
{
    // Time of the last attack (slash); starts at creation so a fresh enemy waits before its first attack
    public DateTime LastAttackTime = DateTime.Now;
}