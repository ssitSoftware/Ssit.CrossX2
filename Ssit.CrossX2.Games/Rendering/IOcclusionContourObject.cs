using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Rendering;

/// <summary>
/// Game object that shows its contour where it's hidden behind the foreground layers
/// (layers above the main layer, scrolled with a speed other than 1). See <see cref="IOcclusionContours"/>.
/// </summary>
public interface IOcclusionContourObject
{
    /// <summary>
    /// Contour color; transparent for no contour (e.g. a dead enemy).
    /// </summary>
    RgbaColor OcclusionContourColor { get; }

    /// <summary>
    /// Draws the object's silhouette into the occlusion mask: only the textures that make its outline, at the same
    /// place as in <see cref="IGameObjectRenderer.Render"/>. The renderer fills them with the contour color.
    /// Mustn't change the object's state (called in addition to the regular rendering).
    /// </summary>
    void RenderSilhouette(IRenderer renderer, float depth);
}
