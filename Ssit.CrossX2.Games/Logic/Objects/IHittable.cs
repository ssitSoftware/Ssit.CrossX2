using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public interface IHittable
{
    Vector2 Position { get; }
    /// <param name="dir">Push direction (away from the attacker).</param>
    /// <param name="power">Damage.</param>
    /// <param name="attacker">Who dealt the hit, if known (e.g. the arrow's shooter).</param>
    void Hit(Vector2 dir, float power, IHittable attacker = null);
    bool Alive { get; }
    bool IsEnemy(ISteeringCharacter hitter);
}