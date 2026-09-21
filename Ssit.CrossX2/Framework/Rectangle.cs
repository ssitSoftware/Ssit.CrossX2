using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Ssit.CrossX2.Framework;

/// <summary>
/// Represents a rectangle defined by its position and size.
/// </summary>
[DebuggerDisplay("Rectangle = ({X}, {Y}, {Width}, {Height})")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public readonly struct Rectangle : IEquatable<Rectangle>
{
    public static readonly Rectangle Empty = new(0, 0, 0, 0);
    
    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the X coordinate of the rectangle's top-left corner.
    /// </summary>
    public readonly int X;

    /// <summary>
    /// The Y-coordinate of the top-left corner of the rectangle.
    /// </summary>
    public readonly int Y;

    /// <summary>
    /// Gets the width of the rectangle.
    /// </summary>
    public readonly int Width;

    /// <summary>
    /// Gets the height of the rectangle.
    /// </summary>
    public readonly int Height;

    /// <summary>
    /// Gets the X coordinate of the rectangle's right edge.
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// Gets the Y coordinate of the rectangle's bottom edge.
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    /// Gets the coordinates of the center of the rectangle.
    /// </summary>
    public Vector2 Center => new(X + Width / 2f, Y + Height / 2f);

    /// <summary>
    /// Gets a value indicating whether the rectangle is empty (has zero width or height).
    /// </summary>
    public bool IsEmpty => Width == 0 || Height == 0;

    public Size Size => new Size(Width, Height);

    /// <summary>
    /// Computes the intersection of this rectangle with another rectangle.
    /// </summary>
    /// <param name="rectangle">The rectangle to intersect with.</param>
    /// <return>
    /// A new <c>Rectangle</c> representing the intersection of this rectangle
    /// and the specified rectangle. If the rectangles do not intersect, a rectangle
    /// with zero width and height is returned.
    /// </return>
    public Rectangle Intersect(Rectangle rectangle)
    {
        var x = Math.Max(X, rectangle.X);
        var y = Math.Max(Y, rectangle.Y);

        var r = Math.Min(Right, rectangle.Right);
        var b = Math.Min(Bottom, rectangle.Bottom);

        var w = Math.Max(0, r - x);
        var h = Math.Max(0, b - y);

        return new Rectangle(x, y, w, h);
    }

    /// <summary>
    /// Determines whether this rectangle intersects with another rectangle.
    /// </summary>
    /// <param name="other">The rectangle to check for intersection with.</param>
    /// <return>
    /// <c>true</c> if the rectangles intersect; otherwise, <c>false</c>.
    /// </return>
    public bool IsIntersecting(Rectangle other)
    {
        if (other.Right <= X) return false;
        if (other.X >= Right) return false;

        if (other.Bottom <= Y) return false;
        return other.Y < Bottom;
    }

    /// <summary>
    /// Expands or contracts the size of this rectangle by the specified amount.
    /// </summary>
    /// <param name="i">The amount by which to inflate the rectangle on each side.</param>
    /// <return>
    /// A new <c>Rectangle</c> that is larger than the current rectangle by twice the specified amount in both width and height,
    /// and shifted by the specified amount to the top-left.
    /// </return>
    public Rectangle Inflate(int i)
    {
        return new Rectangle(X - i, Y - i, Width + i * 2, Height + i * 2);
    }

    /// <summary>
    /// Expands or contracts the rectangle by the specified size.
    /// </summary>
    /// <param name="size">The size by which to inflate the rectangle.</param>
    /// <return>
    /// A new <c>Rectangle</c> representing the inflated rectangle. The size
    /// of the resulting rectangle is increased or decreased by the specified
    /// amount in all directions.
    /// </return>
    public Rectangle Inflate(Size size)
    {
        return new Rectangle(X - size.Width, Y - size.Height, Width + size.Width * 2, Height + size.Height * 2);
    }

    public bool Equals(Rectangle other)
    {
        return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object obj)
    {
        return obj is Rectangle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
    }
    
    public static bool operator ==(Rectangle r1, Rectangle r2) => r1.Equals(r2);
    public static bool operator !=(Rectangle r1, Rectangle r2) => !r1.Equals(r2);
}