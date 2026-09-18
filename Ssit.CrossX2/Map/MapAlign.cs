namespace Ssit.CrossX2.Map;

[Flags]
public enum MapAlign
{
    Left = 0,
    Center = 1,
    Right = 2,
    Top = 0,
    VCenter = 4,
    Bottom = 8,
    
    BottomRight = Bottom | Right,
    BottomCenter = Bottom | Center,
    
    VCenterRight = VCenter | Right,
    VCenterCenter = VCenter | Center,

    Horizontal = Left | Center | Right,
    Vertical = Top | VCenter | Bottom
}