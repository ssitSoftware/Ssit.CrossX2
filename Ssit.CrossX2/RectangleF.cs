using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Ssit.CrossX2;

/// <summary>
/// Represents a rectangle defined by a location (X, Y) and a size (Width, Height) with floating-point precision.
/// </summary>
/// <remarks>
/// This struct is immutable and provides various properties and methods for geometric operations.
/// It supports intersection, inflation, and containment checks.
/// </remarks>
[DebuggerDisplay("RectangleF = ({X}, {Y}, {Width}, {Height})")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public readonly struct RectangleF: IEquatable<RectangleF>
{
    public RectangleF(Vector2 position, SizeF size)
    {
        X = position.X;
        Y = position.Y;

        Width = size.Width;
        Height = size.Height;
    }
    
    public RectangleF(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the <see cref="Vector2"/> that represents the top-left corner of the rectangle.
    /// </summary>
    /// <remarks>
    /// The <see cref="TopLeft"/> property returns the coordinates of the top-left corner (X, Y) of the rectangle.
    /// This property is useful when you need to retrieve the position of the rectangle's starting point.
    /// </remarks>
    public Vector2 TopLeft => new(X, Y);

    /// <summary>
    /// Gets the coordinates of the bottom-right corner of the rectangle.
    /// It is calculated as the point (X + Width, Y + Height).
    /// </summary>
    /// <value>A <see cref="Vector2"/> that represents the bottom-right corner of the rectangle.</value>
    public Vector2 BottomRight => new(Right, Bottom);

    /// <summary>
    /// Gets the coordinates of the bottom-left corner of the rectangle.
    /// </summary>
    /// <value>
    /// A <see cref="Vector2"/> representing the bottom-left corner of the rectangle.
    /// </value>
    public Vector2 BottomLeft => new(X, Bottom);

    /// <summary>
    /// Gets the coordinates of the top-right corner of the rectangle as a <see cref="Vector2"/> structure.
    /// </summary>
    /// <remarks>
    /// The TopRight property calculates the position of the top-right corner using the Right and Y values of the rectangle.
    /// The Right value is derived from the sum of X and Width, providing the horizontal coordinate at the far right of the rectangle.
    /// The Y value is used directly for the vertical coordinate at the top of the rectangle.
    /// </remarks>
    public Vector2 TopRight => new(Right, Y);

    /// <summary>
    /// Gets or sets the center point of the object.
    /// </summary>
    /// <value>
    /// The center point represented as a two-dimensional coordinate.
    /// </value>
    public Vector2 Center => new(X + Width / 2, Y + Height / 2);

    /// <summary>
    /// Gets the size of the rectangle, represented as a <see cref="SizeF"/> instance containing the width and height values.
    /// </summary>
    /// <value>
    /// The <see cref="SizeF"/> structure that represents the width and height of the rectangle.
    /// </value>
    public SizeF Size => new(Width, Height);

    /// <summary>
    /// Represents the X-coordinate of the top-left corner of the rectangle.
    /// </summary>
    /// <remarks>
    /// The X property defines the horizontal position of the rectangle's top-left corner.
    /// This property is crucial for determining the rectangle's placement along the X-axis.
    /// </remarks>
    public readonly float X;

    /// <summary>
    /// Represents the Y coordinate of the top edge of the rectangle.
    /// </summary>
    /// <remarks>
    /// The <see cref="Y"/> property defines the vertical position of the rectangle's starting point.
    /// This property is essential for positioning and geometric calculations involving the rectangle.
    /// </remarks>
    public readonly float Y;

    /// <summary>
    /// Gets the width of the rectangle with floating-point precision.
    /// </summary>
    /// <remarks>
    /// The <see cref="Width"/> property represents the horizontal extent of the rectangle.
    /// This property is used to determine the size of the rectangle along the X-axis.
    /// </remarks>
    public readonly float Width;

    /// <summary>
    /// Gets the height of the rectangle.
    /// </summary>
    /// <remarks>
    /// The <see cref="Height"/> property provides the vertical size of the rectangle with floating-point precision.
    /// This value is assigned when the rectangle is constructed and determines the vertical dimension of the rectangle.
    /// </remarks>
    public readonly float Height;

    /// <summary>
    /// Gets the <see cref="float"/> value representing the right edge of the rectangle.
    /// </summary>
    /// <remarks>
    /// The <see cref="Right"/> property calculates the X-coordinate of the right edge of the rectangle by adding the <see cref="Width"/> to the <see cref="X"/> position.
    /// This property is useful for determining the horizontal boundary of the rectangle.
    /// </remarks>
    public float Right => X + Width;

    /// <summary>
    /// Gets the bottom coordinate of the rectangle.
    /// </summary>
    /// <remarks>
    /// The <see cref="Bottom"/> property returns the Y coordinate plus the height of the rectangle.
    /// This property is useful to determine the vertical boundary of the rectangle.
    /// </remarks>
    public float Bottom => Y + Height;

    /// <summary>
    /// Gets a value indicating whether the rectangle has zero width or height.
    /// </summary>
    /// <remarks>
    /// The <see cref="IsEmpty"/> property returns true if either the width or height of the rectangle is zero.
    /// This property is useful for quickly determining whether a rectangle is effectively non-existent in terms of area.
    /// </remarks>
    public bool IsEmpty => Width == 0 || Height == 0;

    /// <summary>
    /// Computes the intersection of this RectangleF with another specified RectangleF.
    /// </summary>
    /// <param name="rectangle">The RectangleF to intersect with this RectangleF.</param>
    /// <returns>A new RectangleF that represents the intersection of the two rectangles.
    /// If there is no intersection, an empty RectangleF is returned.</returns>
    public RectangleF Intersect(RectangleF rectangle)
    {
        var x = MathF.Max(X, rectangle.X);
        var y = MathF.Max(Y, rectangle.Y);

        var r = MathF.Min(Right, rectangle.Right);
        var b = MathF.Min(Bottom, rectangle.Bottom);

        var w = MathF.Max(0, r - x);
        var h = MathF.Max(0, b - y);

        return new RectangleF(x, y, w, h);
    }

    /// <summary>
    /// Determines if this rectangle intersects with the specified rectangle.
    /// </summary>
    /// <param name="other">The rectangle to check for intersection with this rectangle.</param>
    /// <returns>True if the rectangles intersect; otherwise, false.</returns>
    public bool IsIntersecting(RectangleF other)
    {
        if (other.Right <= X) return false;
        if (other.X >= Right) return false;

        if (other.Bottom <= Y) return false;
        return other.Y < Bottom;
    }

    /// <summary>
    /// Returns a new rectangle that is inflated by the specified amounts on each side.
    /// </summary>
    /// <param name="horz">The amount to inflate the rectangle horizontally.</param>
    /// <param name="vert">The amount to inflate the rectangle vertically.</param>
    /// <returns>A new <see cref="RectangleF"/> that is larger by the specified amounts.</returns>
    public RectangleF Inflate(float horz, float vert) => Inflate(horz, vert, horz, vert);

    /// <summary>
    /// Inflates the rectangle by the specified amount uniformly in all directions.
    /// </summary>
    /// <param name="all">The amount by which to inflate the rectangle on all sides.</param>
    /// <returns>A new RectangleF object that is larger or smaller by the specified amount.</returns>
    public RectangleF Inflate(float all) => Inflate(all, all, all, all);

    /// <summary>
    /// Inflates the rectangle by the specified amount on each side.
    /// </summary>
    /// <param name="left">The amount to inflate the rectangle to the left.</param>
    /// <param name="top">The amount to inflate the rectangle to the top.</param>
    /// <param name="right">The amount to inflate the rectangle to the right.</param>
    /// <param name="bottom">The amount to inflate the rectangle to the bottom.</param>
    /// <returns>A new <see cref="RectangleF"/> that is inflated by the specified amounts.</returns>
    public RectangleF Inflate(float left, float top, float right, float bottom)
    {
        var w = Width + left + right;
        var h = Height + top + bottom;

        var x = X - left;
        var y = Y - top;

        return new RectangleF(x, y, w, h);
    }

    /// <summary>
    /// Determines whether the specified point is contained within this rectangle.
    /// </summary>
    /// <param name="pos">The point to check for containment.</param>
    /// <returns>True if the rectangle contains the specified point; otherwise, false.</returns>
    public bool Contains(Vector2 pos)
    {
        if (pos.X < X) return false;
        if (pos.X >= Right) return false;

        if (pos.Y < Y) return false;
        return pos.Y < Bottom;
    }

    public static implicit operator RectangleF(Rectangle rect) =>
        new(rect.X, rect.Y, rect.Width, rect.Height);

    public static bool operator == (RectangleF r1, RectangleF r2) => r1.Equals(r2);

    public static bool operator !=(RectangleF r1, RectangleF r2) => !r1.Equals(r2);

    public bool Equals(RectangleF other) => X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
    public override bool Equals(object obj) => obj is RectangleF other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
    public RectangleF Offset(Vector2 offset) => new(X + offset.X, Y + offset.Y, Width, Height);
    
    public static RectangleF operator + (RectangleF rect, Vector2 offset) => rect.Offset(offset);

    public RectangleF SetWidth(float width)
    {
        return new RectangleF(X, Y, width, Height);
    }
    
    public RectangleF SetHeight(float height)
    {
        return new RectangleF(X, Y, Height, height);
    }
    
    public RectangleF SetSize(SizeF size)
    {
        return new RectangleF(X, Y, size.Width, size.Height);
    }
    
    public RectangleF SetX(float x)
    {
        return new RectangleF(x, Y, Width, Height);
    }
    
    public RectangleF SetY(float y)
    {
        return new RectangleF(X, y, Width, Height);
    }
    
    public RectangleF SetPosition(Vector2 position)
    {
        return new RectangleF(position.X, position.Y, Width, Height);
    }
}