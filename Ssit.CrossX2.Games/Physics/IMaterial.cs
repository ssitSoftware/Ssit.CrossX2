namespace Ssit.CrossX2.Framework.Games.Physics;

public interface IMaterial
{
    public const ulong AllColliders = ulong.MaxValue;

    static ulong Bit(byte group) => 2UL << group;

    int Index { get; }
    float Friction { get; set; }
    float Bounce { get; set; }
    ColliderSides Sides { get; set; }
    byte ColliderGroup { get; set; }
    ulong CollidesWith { get; set; }
    IMaterial Clone(int? newIndex = null);
}