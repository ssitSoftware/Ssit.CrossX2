namespace Ssit.CrossX2.Framework.Games.Physics;

public interface IMaterial
{
    public const int AllColliders = 0x7ffffff;
    
    int Index { get; }
    float Friction { get; set; }
    float Bounce { get; set; }
    ColliderSides Sides { get; set; }
    int ColliderGroup { get; set; }
    IMaterial Clone(int? newIndex = null);
}