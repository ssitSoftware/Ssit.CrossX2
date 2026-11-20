namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

/// <summary>
/// A character whose melee hits (<see cref="Platformer.Behaviors.SteeringCharacters.SlashBehavior"/>) depend on its
/// weapon - e.g. its sharpness - and wear it.
/// </summary>
public interface IMeleeAttacker
{
    /// <summary>Damage of a melee hit, from the attack event's base <paramref name="power"/>.</summary>
    float GetMeleePower(float power);

    /// <summary>
    /// Called after a melee hit connected with anything - the <paramref name="hittable"/> (null for a wall etc.).
    /// </summary>
    /// <param name="hitTaken">
    /// True when the <paramref name="hittable"/> took the hit (<see cref="IHittable.Hit"/> returned true);
    /// false for a wall or a hit it ignored (e.g. a short invulnerability window).
    /// </param>
    void OnMeleeHit(IHittable hittable, bool hitTaken);
}
