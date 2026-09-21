using Ssit.CrossX2.Framework.UI.Parameters;

namespace Ssit.CrossX2.Framework.UI.Values;

public readonly struct Thickness
{
    public static readonly Thickness Zero = new(Length.Zero, Length.Zero, Length.Zero, Length.Zero);
    
    public Length? Left { get; }
    public Length? Right { get; }
    public Length? Top { get; }
    public Length? Bottom { get; }
    
    private Thickness(Length? left, Length? top, Length? right, Length? bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public static implicit operator Thickness(Length? value) => new(value, value, value, value);

    public static implicit operator Thickness((Length? horizontal, Length? vertical) th) =>
        new(th.horizontal, th.vertical, th.horizontal, th.vertical);

    public static implicit operator Thickness((Length? left, Length? top, Length? right, Length? bottom) th) =>
        new(th.left, th.top, th.right, th.bottom);
}