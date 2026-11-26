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
    Light = 8,
    /// <summary>
    /// Collision mask ("&lt;name&gt;.mask.png", e.g. of a tileset). Used instead of the diffuse map when the shape of the
    /// colliding parts is drawn (occlusion mask, see <see cref="IOcclusionContours.SetSilhouetteMap"/>).
    /// </summary>
    CollisionMask = 16
}