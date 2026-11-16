namespace Ssit.CrossX2.Framework.UI.Values;

public enum ImageScalingMode
{
    None = 0,
    AspectFit = 1,
    AspectFill = 2,
    /// <summary>
    /// Like <see cref="AspectFit"/>, but the size the view doesn't set (Width or Height left Auto)
    /// is calculated from the other one with the image's aspect ratio.
    /// </summary>
    AspectRatio = 3,
    Fill = 4
}