using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.UI.Handlers;

namespace Ssit.CrossX2.UI.Views;

public readonly struct ColorWrapper
{
    private readonly RgbaColor? _color;
    private readonly int? _colorIndex;
    private readonly float _opacity;

    public readonly string ColorId;
    
    private ColorWrapper(RgbaColor? color, int? colorIndex, float opacity = 1, string colorId = null)
    {
        _color = color;
        _colorIndex = colorIndex;
        _opacity = opacity;
        ColorId = colorId;
    }
    
    public RgbaColor? GetColor(IRenderer renderer, IColorSource source = null)
    {
        if (ColorId != null)
        {
            var color = source?.GetColor(ColorId);
            if(color.HasValue) return color.Value * _opacity;
        }

        return _color.Value;
    }
    
    public static implicit operator ColorWrapper(RgbaColor color) => new(color, null);
    public static implicit operator ColorWrapper(int color) => new(null, color);
    public static implicit operator ColorWrapper((int color, string colorId) d) => new(null, d.color, colorId: d.colorId);
    public static implicit operator ColorWrapper((int color, float opacity) d) => new(null, d.color, d.opacity);
}