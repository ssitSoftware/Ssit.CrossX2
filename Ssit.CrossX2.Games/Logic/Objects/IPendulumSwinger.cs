using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public interface IPendulumSwinger
{
    Vector2 Position { get; }
    void OnAttachPosition(Vector2 position);
}
