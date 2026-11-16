namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Captures the content of textures as images.
/// </summary>
public interface ISnapshotService
{
    /// <summary>
    /// Writes the diffuse content of <paramref name="tex"/> to <paramref name="outStream"/> as a JPEG image.
    /// Must be called on the main thread - it waits until the texture is read back from the GPU.
    /// </summary>
    /// <param name="outStream">Stream the JPEG data is written to.</param>
    /// <param name="tex">Texture or render target to capture (e.g. <see cref="Core.IRenderHost.SceneTexture"/>).</param>
    /// <param name="downSizeTimes">How many times the image is shrunk in each dimension (1 = original size, fractions allowed, e.g. 2.5).</param>
    void GetSnapshot(Stream outStream, ITexture tex, float downSizeTimes);
}
