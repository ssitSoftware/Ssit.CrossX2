using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.UI.Values;

public interface IButtonStateColors
{
    RgbaColor? GetColor(IRenderer renderer, bool hover, bool focused, bool pushed, bool enabled, bool isChecked);
}