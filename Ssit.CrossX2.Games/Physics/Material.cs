namespace Ssit.CrossX2.Framework.Games.Physics;

public class Material : IMaterial
{
    public static IMaterial Default { get; } = new Material { Friction = 1, Bounce = 0, Sides = ColliderSides.All, Index = -1 };
    public float Friction { get; set; }
    public float Bounce { get; set; }
    public ColliderSides Sides { get; set; }
    public int ColliderGroup { get; set; } = IMaterial.AllColliders;

    public int Index { get; set; }

    public IMaterial Clone(int? newIndex = null)
    {
        var index = newIndex ?? Index;
        return new Material { Index = index, Bounce = Bounce, Sides = Sides, Friction = Friction, ColliderGroup = ColliderGroup };
    }
}