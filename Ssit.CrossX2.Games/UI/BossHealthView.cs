using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.Games.UI;

/// <summary>
/// Boss health bar.
/// The image (or its <see cref="IImageSource{TResource}.SourceRect"/>) is split vertically into two equal halves:
/// the upper half is the always visible base (frame) of the bar,
/// the lower half is the fill, drawn on top of the base.
/// The bar height is half of the image height.
/// The fill shrinks by cutting its middle out, so both of its ends always stay visible:
/// <code>
/// ABCDEFGHIJ
///   ABCDGHIJ
///     ABCHIJ
/// </code>
/// </summary>
public class BossHealthView: Background
{
    /// <summary>Bar image: base on the upper half, fill on the lower half.</summary>
    public IImageSource<ITexture> Source { get; set; }

    /// <summary>Current HP.</summary>
    public SharedValue<int> Value { get; set; }

    /// <summary>Maximum HP.</summary>
    public SharedValue<int> MaxValue { get; set; }

    /// <summary>
    /// Transparent columns on the left and right side of the fill (in texture pixels),
    /// excluded from the proportional cut so even 1 HP shows a visible fill.
    /// </summary>
    public int? FillInset { get; set; }

    /// <summary>Where the shrunk fill is anchored inside the full fill area.</summary>
    public Align? FillAlign { get; set; }
}
