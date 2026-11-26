using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Games.Utils;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Rendering.Map;

public static class MapRenderer
{
    private readonly struct ObjectRenderInfo
    {
        public readonly MapDisplayObject DisplayObject;
        public readonly IGameObjectRenderer ObjectRenderer;
        public float ZOrder => DisplayObject?.Zorder ?? ObjectRenderer?.ZOrder ?? 0;
        
        public ObjectRenderInfo(MapDisplayObject displayObject)
        {
            DisplayObject = displayObject;
            ObjectRenderer = null;
        }
        
        public ObjectRenderInfo(IGameObjectRenderer objectRenderer)
        {
            DisplayObject = null;
            ObjectRenderer = objectRenderer;
        }

    }
    private static readonly List<ObjectRenderInfo> GameObjects = new();
    
    private static (Vector2 offset, RectangleF bounds) GetLayerRenderParameters(LayerDisplayElement mainLayer, LayerDisplayElement layer, Vector2 cameraLookAt, Size targetSize, int tileSize)
    {
        var screenSizeNormalized = targetSize.ToVector() / tileSize;
        
        var globalOffset = cameraLookAt + new Vector2(-screenSizeNormalized.X / 2, screenSizeNormalized.Y / 2);
        
        globalOffset.X = MathF.Min(MathF.Max(0, globalOffset.X), mainLayer.SourceSize.Width - screenSizeNormalized.X);
        globalOffset.Y = MathF.Min(MathF.Max(screenSizeNormalized.Y, globalOffset.Y), mainLayer.SourceSize.Height);

        var offset = LayerOffsetCalculator.CalculateLayerOffset(layer, mainLayer, globalOffset);
        
        offset = -offset * tileSize + targetSize.ToVector() / 2 + new Vector2(-targetSize.Width / 2f, targetSize.Height / 2f);
        var visibleBounds = new RectangleF(-offset.X / tileSize, -offset.Y / tileSize, (float)targetSize.Width / tileSize, (float)targetSize.Height / tileSize);
        
        return (offset, visibleBounds);
    }
    
    /// <summary>
    /// World-space rectangle (in tiles) of the main layer visible for the given camera position and target size
    /// (camera clamped to the map edges the same way as in rendering).
    /// </summary>
    public static RectangleF GetVisibleBounds(MapDisplayElement map, Vector2 cameraLookAt, Size targetSize, int tileSize)
    {
        var mainLayer = map.Layers.First(o => o.IsMain);
        return GetLayerRenderParameters(mainLayer, mainLayer, cameraLookAt, targetSize, tileSize).bounds;
    }

    public static void RenderDebug(IRenderer renderer, MapDisplayElement map, ISimulation world, Vector2 cameraLookAt,
        Size targetSize, IGameTemplate gameTemplate)
    {
        var tileSize = gameTemplate.TileSize;
        
        var mainLayer = map.Layers.First(o => o.IsMain);
        var (offset, _) = GetLayerRenderParameters(mainLayer, mainLayer, cameraLookAt, targetSize, tileSize);
        
        renderer.StateManager.SaveState();
        renderer.StateManager.Translate(offset);
        renderer.StateManager.Scale(tileSize);
        
        SimulationRenderer.Render(renderer.GeometryRenderer, world);

        renderer.StateManager.RestoreState();
    }

    private delegate void RenderObjects(IRenderer renderer, LayerDisplayElement layer, object world, RectangleF bounds, RgbaColor color, bool front, int renderFilter);
    
    /// <param name="occlusionContours">
    /// Contours of the <see cref="IOcclusionContourObject"/> objects hidden behind the layers drawn after the main layer
    /// with a speed other than 1 (the layers scrolled with the main layer may cover objects on purpose). Null - no contours.
    /// </param>
    public static void Render(IRenderer renderer, MapDisplayElement map, ISimulation world, Vector2 cameraLookAt,
        Size targetSize, IGameTemplate template, int filterMode, OcclusionContoursSettings? occlusionContours = null)
    {
        Render(renderer, map, RenderGameObjectsSim, world, cameraLookAt, targetSize, template, filterMode, occlusionContours);
        RenderPostRenderers(renderer, map, world, cameraLookAt, targetSize, template, filterMode);
    }

    private static void RenderPostRenderers(IRenderer renderer, MapDisplayElement map, ISimulation world, Vector2 cameraLookAt,
        Size targetSize, IGameTemplate template, int filterMode)
    {
        var tileSize = template.TileSize;
        var mainLayer = map.Layers.First(o => o.IsMain);
        var (offset, _) = GetLayerRenderParameters(mainLayer, mainLayer, cameraLookAt, targetSize, tileSize);

        renderer.StateManager.SaveState();
        renderer.StateManager.Translate(offset);

        foreach (var body in world.Bodies)
        {
            if (body.Owner is IGamePostRenderer postRenderer)
            {
                if (filterMode == 0 || filterMode == postRenderer.FilterMode)
                {
                    postRenderer.Render(renderer);
                }
            }
        }

        renderer.StateManager.RestoreState();
    }

    private static void Render(IRenderer renderer, MapDisplayElement map, RenderObjects renderObjects, object world, Vector2 cameraLookAt, Size targetSize, IGameTemplate template, int filterMode,
        OcclusionContoursSettings? occlusionContours = null)
    {
        int tileSize = template.TileSize;
        var mainLayer = map.Layers.First(o => o.IsMain);

        // Contours only in the regular color pass of the full scene.
        var contours = occlusionContours is { Opacity: > 0 } && filterMode == 0 && renderer.CurrentPass == RenderPass.Normal
            ? occlusionContours.Value
            : (OcclusionContoursSettings?)null;

        var afterMainLayer = false;
        var occlusionMaskReady = false;
        
        foreach (var layer in map.Layers)
        {
            var (offset, visibleBounds) = GetLayerRenderParameters(mainLayer, layer, cameraLookAt, targetSize, tileSize);
            
            renderer.StateManager.SaveState();
            renderer.StateManager.Translate(offset);

            if (renderer.CurrentPass == RenderPass.Normal)
            {
                map.LightsProvider.ApplyLights(renderer, layer.UseAmbientLight, layer.UseGlobalLights);
            }

            // Layers scrolled with the main layer cover the objects on purpose - only the other foreground layers show the contours.
            var contoured = contours.HasValue && occlusionMaskReady && afterMainLayer && !IsScrolledWithMainLayer(layer)
                            && renderer.OcclusionContours.BeginContours(contours.Value.Width, contours.Value.Opacity);

            if (layer.IsMain)
            {
                renderObjects(renderer, layer, world, visibleBounds, layer.TintColor, false, filterMode);
            }
            else if (filterMode == 0)
            {
                foreach (var obj in layer.DisplayObjects)
                {
                    if (obj.Zorder > 0)
                    {
                        continue;
                    }
                    
                    DrawObject(renderer, visibleBounds, obj, layer.TintColor);
                }
            }

            if (filterMode == 0)
            {
                RenderLayer(renderer, layer, visibleBounds);
            }

            if (layer.IsMain)
            {
                renderObjects(renderer, layer, world, visibleBounds, layer.TintColor, true, filterMode);

                if (contours.HasValue && world is ISimulation simulation)
                {
                    occlusionMaskReady = RenderOcclusionMask(renderer, layer, simulation, visibleBounds, contours.Value.MainLayerColor);
                }
            }
            else if (filterMode == 0)
            {
                foreach (var obj in layer.DisplayObjects)
                {
                    if (obj.Zorder < 0)
                    {
                        continue;
                    }
                    DrawObject(renderer, visibleBounds, obj, layer.TintColor);
                }
            }

            if (contoured)
            {
                renderer.OcclusionContours.EndContours();
            }
            
            renderer.StateManager.RestoreState();

            if (layer.FogColor.A > 0)
            {
                renderer.GeometryRenderer.FillRectangle(new RectangleF(Vector2.Zero, targetSize), renderer.CurrentPass == RenderPass.Glow ? RgbaColor.Black * layer.FogColor.Af: layer.FogColor);
            }
            
            renderer.LightingManager.EnableLighting(false);

            afterMainLayer |= layer.IsMain;
        }
    }

    private static bool IsScrolledWithMainLayer(LayerDisplayElement layer) =>
        Math.Abs(layer.Speed.X - 1) < float.Epsilon && Math.Abs(layer.Speed.Y - 1) < float.Epsilon;

    private static readonly List<(float zOrder, IOcclusionContourObject obj)> OcclusionObjects = new();

    /// <summary>
    /// Draws the occlusion mask with the main layer transform (the same place as the layer was just drawn):
    /// the colliding parts of the main layer tiles (tileset collision masks) in <paramref name="mainLayerColor"/>,
    /// then the silhouettes of the visible <see cref="IOcclusionContourObject"/> objects over them.
    /// </summary>
    /// <returns>True when the mask was drawn (there is something with a contour).</returns>
    private static bool RenderOcclusionMask(IRenderer renderer, LayerDisplayElement mainLayer, ISimulation simulation, RectangleF bounds, RgbaColor mainLayerColor)
    {
        var depth = mainLayer.Depth;
        var drawTiles = mainLayerColor.A > 0 && HasCollisionMaskTiles(mainLayer, bounds);

        OcclusionObjects.Clear();

        foreach (var body in simulation.Bodies)
        {
            if (body.Owner is IOcclusionContourObject obj and IGameObjectRenderer gor
                && obj.OcclusionContourColor.A > 0
                && gor.Bounds.IsIntersecting(bounds))
            {
                OcclusionObjects.Add((gor.ZOrder, obj));
            }
        }

        if (OcclusionObjects.Count == 0 && !drawTiles)
            return false;

        // Same order as drawn: the silhouette on top gives the contour color where objects overlap.
        OcclusionObjects.Sort((o1, o2) => o1.zOrder.CompareTo(o2.zOrder));

        var occlusion = renderer.OcclusionContours;
        occlusion.BeginMask();

        if (drawTiles)
        {
            // Only the colliding parts: the tileset's collision mask is used instead of its diffuse map.
            occlusion.SetSilhouetteColor(mainLayerColor);
            occlusion.SetSilhouetteMap(TextureMaps.CollisionMask);
            RenderLayer(renderer, mainLayer, bounds, TextureMaps.CollisionMask);
            occlusion.SetSilhouetteMap(TextureMaps.Diffuse);
        }

        foreach (var (_, obj) in OcclusionObjects)
        {
            occlusion.SetSilhouetteColor(obj.OcclusionContourColor);
            obj.RenderSilhouette(renderer, depth);
        }

        occlusion.EndMask();
        OcclusionObjects.Clear();

        return true;
    }

    /// <param name="requiredMap">When set, only the segments whose texture has this map are drawn.</param>
    private static void RenderLayer(IRenderer renderer, LayerDisplayElement layer, RectangleF bounds, TextureMaps? requiredMap = null)
    {
        foreach (var tiles in layer.Tiles)
        {
            if (tiles.bounds.IsIntersecting(bounds))
            {
                foreach (var segment in tiles.segments)
                {
                    if (requiredMap.HasValue && !segment.Texture.Resource.Maps.HasFlag(requiredMap.Value))
                        continue;
                    
                    renderer.RenderQueue.PushVertices(PrimitiveType.Triangles, segment.VertexBuffer, segment.Start, segment.Count, segment.Texture.Resource);
                }
            }
        }
    }

    private static bool HasCollisionMaskTiles(LayerDisplayElement layer, RectangleF bounds)
    {
        foreach (var tiles in layer.Tiles)
        {
            if (!tiles.bounds.IsIntersecting(bounds))
                continue;

            foreach (var segment in tiles.segments)
            {
                if (segment.Texture.Resource.Maps.HasFlag(TextureMaps.CollisionMask))
                    return true;
            }
        }

        return false;
    }

    // ReSharper disable once UnusedParameter.Local
    private static void DrawObject(IRenderer renderer, RectangleF bounds, MapDisplayObject obj, RgbaColor tintColor)
    {
        // TODO: Filter invisible elements
        if (obj.Texture is not null)
        {
            renderer.SpriteRenderer.Draw(obj.Texture, obj.Position, null, obj.Origin, color: tintColor, imageTransform: obj.IsFlipped ? ImageTransform.FlipHorizontal : ImageTransform.None, depth: obj.Depth);
        }

        if (obj.SpriteInstance is not null)
        {
            renderer.SpriteRenderer.Draw(obj.SpriteInstance, obj.Position, color: tintColor, transform: obj.IsFlipped ? ImageTransform.FlipHorizontal : ImageTransform.None, depth: obj.Depth);
        }
    }
    
    private static void RenderGameObjectsSim(IRenderer renderer, LayerDisplayElement layer,
        object worldObj, RectangleF bounds, RgbaColor color, bool front, int filterMode)
    {
        if (worldObj is not ISimulation simulation)
            return;
        
        GameObjects.Clear();
        foreach (var obj in layer.DisplayObjects)
        {
            bool isFront = obj.Zorder >= 0;

            if (isFront == front && filterMode == 0)
            {
                GameObjects.Add(new ObjectRenderInfo(obj));
            }
        }
        
        foreach (var body in simulation.Bodies)
        {
            if (body.Owner is IGameObjectRenderer gor && gor.Bounds.IsIntersecting(bounds))
            {
                bool isFront = gor.ZOrder >= 0;

                if (isFront == front && (filterMode == 0 || filterMode == gor.FilterMode))
                {
                    GameObjects.Add(new ObjectRenderInfo(gor));
                }
            }
        }
        
        GameObjects.Sort((o1,o2) => o1.ZOrder.CompareTo(o2.ZOrder));
        foreach (var gameObj in GameObjects)
        {
            if (gameObj.DisplayObject != null)
            {
                DrawObject(renderer, bounds, gameObj.DisplayObject, layer.TintColor);
            }

            gameObj.ObjectRenderer?.Render(renderer, color, layer.Depth);
        }
        
        GameObjects.Clear();
    }
}