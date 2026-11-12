namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// GPU texture. Besides the diffuse (color) map it may contain glow and light maps.
/// </summary>
public interface ITexture: IDisposable
{
    /// <summary>
    /// Maps contained in this texture.
    /// </summary>
    TextureMaps Maps { get; }
    /// <summary>
    /// Size in pixels.
    /// </summary>
    Size Size { get; }
}