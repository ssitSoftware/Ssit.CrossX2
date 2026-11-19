using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public interface IHittable
{
    Vector2 Position { get; }
    /// <param name="dir">Push direction (away from the attacker).</param>
    /// <param name="power">Damage.</param>
    /// <param name="attacker">Who dealt the hit, if known (e.g. the arrow's shooter).</param>
    /// <returns>True when the hit was taken; false when it was ignored (e.g. a short invulnerability window).</returns>
    bool Hit(Vector2 dir, float power, IHitter attacker = null);
    bool Alive { get; }
    bool IsEnemy(ISteeringCharacter hitter);
}

public interface IHitter
{
    bool Alive { get; }
    bool IsEnemy(ISteeringCharacter hitter);
    Vector2 Position { get; }
}

public interface IHitter<out TKind> : IHitter where TKind : struct
{
    TKind Kind { get; }
} 