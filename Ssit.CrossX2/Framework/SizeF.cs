using System.Diagnostics;
using System.Numerics;

namespace Ssit.CrossX2.Framework;

[DebuggerDisplay("Size = ({Width}, {Height})")]
public readonly struct SizeF
{
    public static readonly SizeF Empty = new (0, 0);

    public readonly float Width;
    public readonly float Height;
    
    public SizeF(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public Vector2 ToVector() => new(Width, Height);
    
    public static implicit operator SizeF(Vector2 size) => new(size.X, size.Y);
    public static implicit operator SizeF(Size size) => new(size.Width, size.Height);
    
    public static SizeF operator * (SizeF size, float multiply) => new(size.Width * multiply, size.Height * multiply);
    public static SizeF operator / (SizeF size, float divide) => new(size.Width / divide, size.Height / divide);
}