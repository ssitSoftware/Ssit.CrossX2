namespace Ssit.CrossX2.Framework.Games.Physics;

[Flags]
public enum ColliderSides
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    Right = 8,
    All = Top | Bottom| Left | Right,
}