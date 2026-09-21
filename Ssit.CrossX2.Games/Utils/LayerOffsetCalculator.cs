using System.Numerics;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Games.Rendering.Map;

namespace Ssit.CrossX2.Framework.Games.Utils;

public static class LayerOffsetCalculator
{
    public static Vector2 CalculateLayerOffset(MapLayer layer, MapLayer mainLayer, Vector2 globalOffset)
    {
        var offX = globalOffset.X * layer.HorizontalSpeed;
        
        var vertSpeed = MathF.Max(0.0001f, layer.VerticalSpeed);
        var offY = globalOffset.Y * vertSpeed - mainLayer.Height * vertSpeed + layer.Height;

        return new Vector2(offX, offY);
    }
    
    public static Vector2 CalculateLayerOffset(LayerDisplayElement layer, LayerDisplayElement mainLayer, Vector2 globalOffset)
    {
        var offX = globalOffset.X * layer.Speed.X;
        
        var vertSpeed = MathF.Max(0.000001f, layer.Speed.Y);
        var offY = globalOffset.Y * vertSpeed - mainLayer.SourceSize.Height * vertSpeed + layer.SourceSize.Height;

        return new Vector2(offX, offY);
    }
    
    public static Vector2 CalculateGlobalOffset(MapLayer layer, MapLayer mainLayer, Vector2 localOffset)
    {
        var horzSpeed = MathF.Max(0.0001f, layer.HorizontalSpeed);
        var vertSpeed = MathF.Max(0.0001f, layer.VerticalSpeed);
        
        var offX = localOffset.X / horzSpeed;
        var offY = mainLayer.Height - (layer.Height - localOffset.Y) / vertSpeed;

        return new Vector2(offX, offY);
    }
}