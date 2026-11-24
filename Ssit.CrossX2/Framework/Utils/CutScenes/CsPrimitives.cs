using System.Globalization;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.Utils.CutScenes;

public record struct CsTime(double Time)
{
    public static implicit operator CsTime(string text) => new(TimeSpan.Parse(text, CultureInfo.InvariantCulture).TotalSeconds);
    public static implicit operator CsTime(TimeSpan time) => new(time.TotalSeconds);
    public static implicit operator CsTime(double time) => new(time);
}

public record struct CsTextParameters(ColorWrapper TextColor, ColorWrapper OutlineColor, FontDesc? Font = null)
{
    public static implicit operator CsTextParameters((ColorWrapper, ColorWrapper, FontDesc) text) => new(text.Item1, text.Item2, text.Item3);
    public static implicit operator CsTextParameters((ColorWrapper, ColorWrapper) text) => new(text.Item1, text.Item2);
    public static implicit operator CsTextParameters(FontDesc fd) => new(null, null, fd);
}