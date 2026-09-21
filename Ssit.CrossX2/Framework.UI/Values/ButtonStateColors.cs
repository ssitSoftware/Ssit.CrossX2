using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.UI.Values;

public class ButtonStateColors: IButtonStateColors
{
    public RgbaColor? Normal;
    public RgbaColor? Hover;
    public RgbaColor? Focused;
    public RgbaColor? Pushed;
    public RgbaColor? Disabled;
    public RgbaColor? Checked;
    
    public RgbaColor? NormalGlow;
    public RgbaColor? HoverGlow;
    public RgbaColor? FocusedGlow;
    public RgbaColor? PushedGlow;
    public RgbaColor? DisabledGlow;
    public RgbaColor? CheckedGlow;

    public RgbaColor? GetColor(IRenderer renderer, bool hover, bool focused, bool pushed, bool enabled, bool isChecked)
    {
        if (renderer.CurrentPass == RenderPass.Glow)
        {
            return GetColorGlow(hover, focused, pushed, enabled, isChecked);
        }
        
        if (!enabled)
        {
            return Disabled ?? Normal;
        }

        if (pushed)
        {
            return Pushed ?? Normal;
        }

        if (hover)
        {
            return Hover ?? Normal;
        }

        if (focused)
        {
            return Focused ?? Normal;
        }

        if (isChecked)
        {
            return Checked ?? Normal;
        }

        return Normal;
    }
    
    private RgbaColor? GetColorGlow(bool hover, bool focused, bool pushed, bool enabled, bool isChecked)
    {
        if (!enabled)
        {
            return DisabledGlow ?? (Disabled?.A > 0 ? RgbaColor.Black : null);
        }

        if (pushed)
        {
            return PushedGlow ?? (Pushed?.A > 0 ? RgbaColor.Black : null);
        }

        if (hover)
        {
            return HoverGlow ?? (Hover?.A > 0 ? RgbaColor.Black : null);
        }

        if (focused)
        {
            return FocusedGlow ?? (Focused?.A > 0 ? RgbaColor.Black : null);
        }
        
        if (isChecked)
        {
            return CheckedGlow ?? (Checked?.A > 0 ? RgbaColor.Black : null);
        }

        return NormalGlow ?? (Normal?.A > 0 ? RgbaColor.Black : null);
    }
}