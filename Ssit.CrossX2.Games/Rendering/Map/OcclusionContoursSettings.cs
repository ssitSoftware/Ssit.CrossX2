namespace Ssit.CrossX2.Framework.Games.Rendering.Map;

/// <summary>
/// Contours of the objects (<see cref="IOcclusionContourObject"/>) hidden behind the foreground layers.
/// </summary>
/// <param name="Width">Contour width in pixels of the render target.</param>
/// <param name="Opacity">Contour opacity over the foreground (0 - 1).</param>
/// <param name="MainLayerColor">
/// Contour color of the main layer's colliding parts - the tiles drawn from their tileset's collision mask
/// (<see cref="Ssit.CrossX2.Framework.Graphics.TextureMaps.CollisionMask"/>). Transparent (default) - no main layer contours.
/// </param>
public readonly record struct OcclusionContoursSettings(float Width, float Opacity, RgbaColor MainLayerColor = default);
