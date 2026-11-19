using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.Games.UI;

/// <summary>
/// Displays the amount of coins: an icon with the amount next to it.
/// The text is vertically centered to the icon.
/// </summary>
public class CoinMeterView: Background
{
    /// <summary>Path to the coin icon texture. Required. The texture is reloaded when the text changes.</summary>
    public SharedString IconTexturePath { get; set; }

    /// <summary>Amount of coins to display.</summary>
    public SharedValue<int> Value { get; set; }

    /// <summary>If true, text is placed on the right of the icon; otherwise on the left.</summary>
    public bool TextOnRight { get; set; } = true;

    /// <summary>
    /// Minimum number of digits displayed. The value is padded with leading zeros to reach it
    /// (e.g. 3 shows 7 as "007"). Values below 1 behave like 1.
    /// </summary>
    public int MinDigits { get; set; } = 1;

    /// <summary>Space between the icon and the text.</summary>
    public Length Spacing { get; set; }

    /// <summary>Font used for the amount. Required. FontSize 0 picks the font's native size.</summary>
    public FontDesc? Font { get; set; }

    /// <summary>Opacity of the whole view (icon and text), 0..1.</summary>
    public SharedValue<float> Opacity { get; set; }

    /// <summary>Color of the amount text.</summary>
    public ColorWrapper? TextColor { get; set; } = RgbaColor.White;

    /// <summary>Outline color of the amount text.</summary>
    public ColorWrapper? TextOutlineColor { get; set; } = RgbaColor.Black;
}
