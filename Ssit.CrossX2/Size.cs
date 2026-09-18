using System.Diagnostics;
using System.Numerics;

namespace Ssit.CrossX2;

[DebuggerDisplay("Size = ({Width}, {Height})")]
public readonly struct Size : IEquatable<Size>
{
    public static readonly Size Zero = new (0, 0);

    public readonly int Width;
    public readonly int Height;
    
    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public Vector2 ToVector() => new(Width, Height);

    public bool Equals(Size other)
    {
        return Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object obj)
    {
        return obj is Size other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }
    
    public static bool operator ==(Size s1, Size s2) => s1.Width == s2.Width && s1.Height == s2.Height;
    public static bool operator !=(Size s1, Size s2) => !(s1 == s2);

    public static Size operator /(Size s, int v) => new(s.Width / v, s.Height / v);
    public static Size operator *(Size s, int v) => new(s.Width * v, s.Height * v);
    
    public static implicit operator  Size ((int w, int h) v) => new(v.w, v.h);
}