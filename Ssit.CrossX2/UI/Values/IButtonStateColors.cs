using Ssit.CrossX2.Graphics;

namespace Ssit.CrossX2.UI.Values;

public interface IButtonStateColors
{
    RgbaColor? GetColor(IRenderer renderer, bool hover, bool focused, bool pushed, bool enabled, bool isChecked);
}