namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Maps a texture can contain.
/// </summary>
[Flags]
public enum TextureMaps
{
    /// <summary>
    /// Base color.
    /// </summary>
    Diffuse = 1,
    /// <summary>
    /// Emissive parts rendered in the glow pass.
    /// </summary>
    Glow = 2,
    /// <summary>
    /// Lighting map (how the surface reacts to dynamic lights).
    /// </summary>
    Light = 8
}