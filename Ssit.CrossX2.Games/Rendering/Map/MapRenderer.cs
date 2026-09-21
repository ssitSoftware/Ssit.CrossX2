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
        public int ZOrder => DisplayObject?.Zorder ?? ObjectRenderer?.ZOrder ?? 0;
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
    
    public static void RenderDebug(IRenderer renderer, MapDisplayElement map, ISimulation world, Vector2 cameraLookAt,
        Size targetSize, IGameTemplate gameTemplate)
    {
        var tileSize = gameTemplate.TileSize;
        
        var mainLayer = map.Layers.First(o => o.IsMain);
        var (offset, _) = GetLayerRenderParameters(mainLayer, mainLayer, cameraLookAt, targetSize, tileSize);

        float scale = 10000f;
        if (((IRenderStateProvider)renderer.StateManager).Scale > 3)
        {
            scale = 1;
        }
        
        renderer.StateManager.SaveState();
        renderer.StateManager.Translate(offset);
        renderer.StateManager.Scale(tileSize);
        
        scale *= ((IRenderStateProvider)renderer.StateManager).Scale;
        SimulationRenderer.RenderScale = scale;
        
        SimulationRenderer.Render(renderer.GeometryRenderer, world);

        renderer.StateManager.RestoreState();
    }

    private delegate void RenderObjects(IRenderer renderer, LayerDisplayElement layer, object world, RectangleF bounds, RgbaColor color, bool front);
    
    public static void Render(IRenderer renderer, MapDisplayElement map, ISimulation world, Vector2 cameraLookAt,
        Size targetSize, int tileSize) =>
        Render(renderer, map, RenderGameObjectsSim, world, cameraLookAt, targetSize, tileSize);

    private static void Render(IRenderer renderer, MapDisplayElement map, RenderObjects renderObjects, object world, Vector2 cameraLookAt, Size targetSize, int tileSize)
    {
        var mainLayer = map.Layers.First(o => o.IsMain);
        
        foreach (var layer in map.Layers)
        {
            var (offset, visibleBounds) = GetLayerRenderParameters(mainLayer, layer, cameraLookAt, targetSize, tileSize);
            
            renderer.StateManager.SaveState();
            renderer.StateManager.Translate(offset);
            
            if (layer.IsMain)
            {
                renderObjects(renderer, layer, world, visibleBounds, layer.TintColor, false);
            }
            else
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
            
            RenderLayer(renderer, layer, visibleBounds);

            if (layer.IsMain)
            {
                renderObjects(renderer, layer, world, visibleBounds, layer.TintColor, true);
            }
            else
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

            renderer.StateManager.RestoreState();
            
            if (layer.FogColor.A > 0)
            {
                renderer.GeometryRenderer.FillRectangle(new RectangleF(Vector2.Zero, targetSize), renderer.CurrentPass == RenderPass.Glow ? RgbaColor.Black * layer.FogColor.Af: layer.FogColor);
            }
        }
    }

    private static void RenderLayer(IRenderer renderer, LayerDisplayElement layer, RectangleF bounds)
    {
        foreach (var tiles in layer.Tiles)
        {
            if (tiles.bounds.IsIntersecting(bounds))
            {
                foreach (var segment in tiles.segments)
                {
                    // TODO: Change to vertex  buffer renderer
                    //renderer.QuadsRenderer.Draw(segment.Texture.Resource, segment.Quads, layer.TintColor);
                }
            }
        }
    }

    // ReSharper disable once UnusedParameter.Local
    private static void DrawObject(IRenderer renderer, RectangleF bounds, MapDisplayObject obj, RgbaColor tintColor)
    {
        // TODO: Filter invisible elements
        if (obj.Texture is not null)
        {
            renderer.SpriteRenderer.Draw(obj.Texture, obj.Position, null, obj.Origin, color: tintColor,  imageTransform: obj.IsFlipped ? ImageTransform.FlipHorizontal : ImageTransform.None);
        }

        if (obj.SpriteInstance is not null)
        {
            renderer.SpriteRenderer.Draw(obj.SpriteInstance, obj.Position, color: tintColor, transform: obj.IsFlipped ? ImageTransform.FlipHorizontal : ImageTransform.None);
        }
    }
    
    private static void RenderGameObjectsSim(IRenderer renderer, LayerDisplayElement layer,
        object worldObj, RectangleF bounds, RgbaColor color, bool front)
    {
        if (worldObj is not ISimulation simulation)
            return;
        
        GameObjects.Clear();
        foreach (var obj in layer.DisplayObjects)
        {
            bool isFront = obj.Zorder >= 0;

            if (isFront == front)
            {
                GameObjects.Add(new ObjectRenderInfo(obj));
            }
        }
        
        foreach (var body in simulation.Bodies)
        {
            if (body.Owner is IGameObjectRenderer gor && gor.Bounds.IsIntersecting(bounds))
            {
                bool isFront = gor.ZOrder >= 0;

                if (isFront == front)
                {
                    GameObjects.Add(new ObjectRenderInfo(gor));
                }
            }
        }
        
        GameObjects.Sort((o1,o2) => o1.ZOrder - o2.ZOrder);
        foreach (var gameObj in GameObjects)
        {
            if (gameObj.DisplayObject != null)
            {
                DrawObject(renderer, bounds, gameObj.DisplayObject, layer.TintColor);
            }

            gameObj.ObjectRenderer?.Render(renderer, color);
        }
        
        GameObjects.Clear();
    }
}