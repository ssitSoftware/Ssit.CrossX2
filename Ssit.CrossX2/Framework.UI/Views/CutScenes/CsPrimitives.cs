using System.Globalization;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views.CutScenes;

public record struct CsPosition(ContentAlign Align, Length? X = null, Length? Y = null)
{
    public static implicit operator CsPosition(ContentAlign align) => new(align);
    public static implicit operator CsPosition((ContentAlign, Length?, Length?) pos) => new(pos.Item1, pos.Item2, pos.Item3);
}

public record struct CsTime(double Time1, double Time2)
{
    public static implicit operator CsTime((string, string) text) => new(TimeSpan.Parse(text.Item1, CultureInfo.InvariantCulture).TotalSeconds, TimeSpan.Parse(text.Item2, CultureInfo.InvariantCulture).TotalSeconds);
    public static implicit operator CsTime((TimeSpan, TimeSpan) time) => new(time.Item1.TotalSeconds, time.Item2.TotalSeconds);
    public static implicit operator CsTime((double, double) time) => new(time.Item1, time.Item2);
}

public record struct CsTextParameters(ColorWrapper TextColor, ColorWrapper OutlineColor, FontDesc? Font = null)
{
    public static implicit operator CsTextParameters((ColorWrapper, ColorWrapper, FontDesc) text) => new(text.Item1, text.Item2, text.Item3);
    public static implicit operator CsTextParameters((ColorWrapper, ColorWrapper) text) => new(text.Item1, text.Item2);
    public static implicit operator CsTextParameters(FontDesc fd) => new(null, null, fd);
}