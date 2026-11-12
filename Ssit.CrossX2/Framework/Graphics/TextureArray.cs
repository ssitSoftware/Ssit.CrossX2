namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// A set of textures loaded together; disposing it disposes all of them. Entries may be null.
/// </summary>
public class TextureArray(ITexture[] textures) : IDisposable
{
    /// <summary>
    /// The textures, e.g. one per palette color (see <see cref="TextureArrayLoader"/>).
    /// </summary>
    public ITexture[] Textures { get; } = textures;

    public void Dispose()
    {
        foreach (var texture in Textures)
        {
            texture?.Dispose();
        }
    }
}