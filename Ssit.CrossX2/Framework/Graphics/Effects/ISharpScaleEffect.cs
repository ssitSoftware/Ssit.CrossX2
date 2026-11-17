namespace Ssit.CrossX2.Framework.Graphics.Effects;

/// <summary>
/// Upscales a texture into a rectangle of the current render target with "sharp bilinear" filtering:
/// pixel-art texels stay solid blocks at non-integer scales, with only their one-pixel seams blended.
/// </summary>
internal interface ISharpScaleEffect: IDisposable
{
    void Render(ITexture source, RectangleF destination);
}
