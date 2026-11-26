namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Contours of objects hidden behind the layers drawn over them.
/// <para>
/// The silhouettes of the objects are drawn into an occlusion mask (<see cref="BeginMask"/> … <see cref="EndMask"/>),
/// then the textured draws made between <see cref="BeginContours"/> and <see cref="EndContours"/> replace their pixels
/// covering the edges of the silhouettes with the contour color. Only the covered part of an object gets the contour.
/// </para>
/// </summary>
public interface IOcclusionContours
{
    /// <summary>
    /// Starts drawing into the occlusion mask (the size of the current render target, cleared to transparent).
    /// Textured draws fill their coverage (texture alpha) with the <see cref="SetSilhouetteColor"/> color until <see cref="EndMask"/>.
    /// Use the same render state (transform) as when drawing the objects normally.
    /// </summary>
    void BeginMask();

    /// <summary>
    /// Color of the silhouettes drawn next into the occlusion mask - the color of their contour.
    /// </summary>
    void SetSilhouetteColor(RgbaColor color);

    /// <summary>
    /// Texture map giving the shape of the silhouettes drawn next into the occlusion mask:
    /// <see cref="TextureMaps.Diffuse"/> (default after <see cref="BeginMask"/>) or <see cref="TextureMaps.CollisionMask"/>
    /// (e.g. tiles - only their colliding parts). Textures without that map draw nothing.
    /// </summary>
    void SetSilhouetteMap(TextureMaps map);

    /// <summary>
    /// Finishes the occlusion mask and continues drawing into the render target that was current in <see cref="BeginMask"/>.
    /// </summary>
    void EndMask();

    /// <summary>
    /// Textured draws until <see cref="EndContours"/> blend the contour color into their pixels on the edges of the mask silhouettes.
    /// </summary>
    /// <param name="width">Contour width in pixels of the render target.</param>
    /// <param name="opacity">Contour opacity over the covering pixels (0 - 1).</param>
    /// <returns>False when there is no mask for the current render target (nothing is changed).</returns>
    bool BeginContours(float width, float opacity);

    /// <summary>
    /// Ends <see cref="BeginContours"/>. Does nothing when contours are not active.
    /// </summary>
    void EndContours();
}
