using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Utils;

public readonly struct HitTestResult
{
    public static readonly HitTestResult None = new();

    public bool HitAnything { get; init; }
    public ICollider Collider { get; init; }
    public IHittable Hittable { get; init; }
}
