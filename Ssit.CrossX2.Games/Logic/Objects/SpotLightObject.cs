using System.Numerics;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Lighting;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class SpotLightObject: SpriteGameObject2, ILightProvider, IGameObjectRenderer
{
    public class Parameters
    {
        [Editor] public RgbaColor Color { get; set; } = RgbaColor.White;
        [EditorFloat(0.1f, 4f, 0.1f)] public float Intensity { get; set; } = 1;
        [EditorFloat(0.1f, 1f, 0.1f)] public float AfterGlow { get; set; } = 0.5f;
    }

    protected record AdditionalParameters(Vector2 Direction, float Range, float Angle1, float Angle2, float BulbSize);

    private const int GlowFanSegments = 4;
    private const int GlowFanRings = 2;
    
    private readonly float _tileSize;
    
    private readonly Vector2[,] _glowFan = new Vector2[GlowFanRings + 1, GlowFanSegments + 1];
    private readonly float _afterGlow;
    
    private SpotLight _spotLight;

    protected SpotLightObject(GameObjectsServices services, ObjectCreationParameters<Parameters> parameters,
        AdditionalParameters additional): base(services, parameters)
    {
        Body.IsKinematic = true;
        Body.Mass = 100000;
        ZOrder = parameters.ZOrder;
        
        _tileSize = services.GameTemplate.TileSize;
        _afterGlow = parameters.Parameters.AfterGlow;
        var bulbRadius = additional.BulbSize * _tileSize;

        var innerAngle = MathF.Min(additional.Angle1, additional.Angle2);
        var outerAngle = MathF.Max(additional.Angle1, additional.Angle2);

        _spotLight = new SpotLight(
            new Vector3(Body.Position, -10) * _tileSize,
            additional.Direction, outerAngle, innerAngle,
            additional.Range * services.GameTemplate.TileSize,
            parameters.Parameters.Color,
            parameters.Parameters.Intensity,
            bulbRadius);
        
        UpdateGlowArc();
    }

    protected void UpdateLight(Vector2? direction = null, RgbaColor? color = null, float? intensity = null)
    {
        _spotLight.Position = new Vector3(Body.Position, -10) * _tileSize;
        _spotLight.Direction = direction ?? _spotLight.Direction;
        _spotLight.Intensity = intensity ?? _spotLight.Intensity;
        _spotLight.Color = color ?? _spotLight.Color;
    }

    public virtual void FillLights(ILightsContainer lightsContainer)
    {
        lightsContainer.AddSpotLight(_spotLight);
    }

    protected void RenderGlow(IRenderer renderer, RgbaColor color, float depth)
    {
        if (renderer.CurrentPass != RenderPass.Glow)
        {
            return;
        }

        renderer.StateManager.SaveState();
        renderer.StateManager.SetBlendMode(BlendMode.AlphaBlend);
        
        var glowColor = _spotLight.Color * (_spotLight.Intensity * _afterGlow / 8f);

        for (var r = 0; r < GlowFanRings; r++)
        {
            var innerT = (float)r / GlowFanRings;
            var outerT = (float)(r + 1) / GlowFanRings;

            var innerColor = glowColor * ((1 - innerT) * (1 - innerT));
            var outerColor = glowColor * ((1 - outerT) * (1 - outerT));

            for (var i = 0; i < GlowFanSegments; i++)
            {
                var innerA = new VertexPct(_glowFan[r, i], innerColor, Vector2.Zero, depth);
                var innerB = new VertexPct(_glowFan[r, i + 1], innerColor, Vector2.Zero, depth);
                var outerA = new VertexPct(_glowFan[r + 1, i], outerColor, Vector2.Zero, depth);
                var outerB = new VertexPct(_glowFan[r + 1, i + 1], outerColor, Vector2.Zero, depth);

                renderer.RenderQueue.PushTriangle(innerA, outerA, outerB);
                renderer.RenderQueue.PushTriangle(innerA, outerB, innerB);
            }
        }
        
        renderer.StateManager.RestoreState();
    }

    protected void UpdateGlowArc()
    {
        var apex = new Vector2(_spotLight.Position.X, _spotLight.Position.Y);

        var halfAngle = _spotLight.OuterAngle * MathF.PI / 180f;
        var baseAngle = MathF.Atan2(_spotLight.Direction.Y, _spotLight.Direction.X);
        var maxRadius = _spotLight.Radius * 0.75f;
        var bulbRadius = _spotLight.BulbRadius;
        
        for (var r = 0; r <= GlowFanRings; r++)
        {
            var radius = bulbRadius + (maxRadius - bulbRadius) * r / GlowFanRings;

            for (var i = 0; i <= GlowFanSegments; i++)
            {
                var t = (float)i / GlowFanSegments;
                var angle = baseAngle - halfAngle + t * (2f * halfAngle);

                _glowFan[r, i] = apex + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            }
        }

        UpdateBounds();
    }

    private void UpdateBounds()
    {
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;

        for (var r = 0; r <= GlowFanRings; r++)
        {
            for (var i = 0; i <= GlowFanSegments; i++)
            {
                var point = _glowFan[r, i];
                minX = MathF.Min(minX, point.X);
                minY = MathF.Min(minY, point.Y);
                maxX = MathF.Max(maxX, point.X);
                maxY = MathF.Max(maxY, point.Y);
            }
        }

        minX /= _tileSize; minY /= _tileSize; maxX /= _tileSize; maxY /= _tileSize;
        
        minX -= Body.Position.X;
        minY -= Body.Position.Y;
        
        maxX -= Body.Position.X;
        maxY -= Body.Position.Y;
        
        BoundsRect = new RectangleF(minX, minY, maxX - minX, maxY - minY);
    }

    public void Dispose()
    {
    }
}