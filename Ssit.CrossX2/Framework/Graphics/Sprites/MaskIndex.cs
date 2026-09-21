namespace Ssit.CrossX2.Framework.Graphics.Sprites;

[Flags]
public enum MaskIndex
{
    None = 0,
    Black = 1 << 0,
    Red = 1 << 1,
    Green = 1 << 2,
    Blue = 1 << 3,
    Yellow = 1 << 4,
    Cyan = 1 << 5,
    Magenta = 1 << 6,
    All = 255
}