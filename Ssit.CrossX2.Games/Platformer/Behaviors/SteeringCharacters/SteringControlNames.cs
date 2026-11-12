namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Names of the input controls read by the steering behaviors via <c>SteeringInput</c>.
/// </summary>
public static class SteeringControlNames
{
    public const string HorizontalMove = nameof(HorizontalMove);
    public const string VerticalMove = nameof(VerticalMove);
    
    // Hold to grab/continue sliding on a wall
    public const string WallSlide = nameof(WallSlide);
    
    public const string Jump = nameof(Jump);
    public const string Attack = nameof(Attack);
    public const string AltAttack = nameof(AltAttack);
    public const string Bow = nameof(Bow);
    public const string Shoot = nameof(Shoot);
    
    // Hold to move at walk speed instead of run speed
    public const string Walk = nameof(Walk);
}