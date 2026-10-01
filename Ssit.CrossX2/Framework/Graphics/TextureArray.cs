namespace Ssit.CrossX2.Framework.Graphics;

public class TextureArray(ITexture[] textures) : IDisposable
{
    public ITexture[] Textures { get; } = textures;

    public void Dispose()
    {
        foreach (var texture in Textures)
        {
            texture?.Dispose();
        }
    }
}