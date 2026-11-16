using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.Games.UI;

/// <summary>
/// Displays HP as a row of hearts.
/// The texture is a horizontal strip of <see cref="FillLevels"/> frames,
/// each <see cref="TextureColumnWidth"/> wide: frame 0 = empty heart, last frame = full heart.
/// </summary>
public class PointMeterView: Background
{
    /// <summary>Path to the hearts texture strip.</summary>
    public string PointsTexturePath { get; set; }

    /// <summary>Width of one frame (column) in the texture, in texture pixels.</summary>
    public int TextureColumnWidth { get; set; }

    /// <summary>Current HP.</summary>
    public SharedValue<int> Value { get; set; }

    /// <summary>Maximum HP – decides how many hearts are displayed.</summary>
    public SharedValue<int> MaxValue { get; set; }

    /// <summary>Number of frames in one heart (including the empty one).</summary>
    public int FillLevels { get; set; }

    /// <summary>How much HP one fill level is worth.</summary>
    public int FillValue { get; set; } = 1;

    /// <summary>Space between hearts.</summary>
    public Length Spacing { get; set; } = Length.Zero;
}
