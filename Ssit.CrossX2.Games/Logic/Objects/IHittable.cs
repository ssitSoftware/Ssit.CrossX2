using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public interface IHittable
{
    Vector2 Position { get; }
    bool Hit(Vector2 dir, float power);
    bool Alive { get; }
    bool IsEnemy(ISteeringCharacter hitter) => true;
}