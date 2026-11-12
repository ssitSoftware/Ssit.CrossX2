namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// A pair of textures generated from one source image: the fill and its outline, so they can be tinted separately
/// (used e.g. by UI icons and sliders). Disposing disposes both textures.
/// </summary>
public class DualTexture(ITexture texture, ITexture outline) : IDisposable
{
    /// <summary>
    /// Fill (body) part of the image.
    /// </summary>
    public ITexture Texture { get; } = texture;
    /// <summary>
    /// Outline part of the image.
    /// </summary>
    public ITexture Outline { get; } = outline;

    public void Dispose()
    {
        Texture?.Dispose();
        Outline?.Dispose();
    }
}