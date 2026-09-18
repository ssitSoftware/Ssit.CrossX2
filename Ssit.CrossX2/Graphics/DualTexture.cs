namespace Ssit.CrossX2.Graphics;

public class DualTexture(ITexture texture, ITexture outline) : IDisposable
{
    public ITexture Texture { get; } = texture;
    public ITexture Outline { get; } = outline;

    public void Dispose()
    {
        Texture?.Dispose();
        Outline?.Dispose();
    }
}