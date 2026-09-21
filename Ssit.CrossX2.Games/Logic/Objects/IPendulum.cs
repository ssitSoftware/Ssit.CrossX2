using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public interface IPendulum
{
    void AppendVelocity(float velocity);
    bool CanAttach(Aabb swingerAabb);
    void AttachObject(IPendulumSwinger swinger);
    void DetachObject(IPendulumSwinger swinger);
    Aabb GetBoundingBox();
    Vector2 AnchorPosition { get; }
}
