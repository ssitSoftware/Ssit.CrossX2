using System.Numerics;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Lighting;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public class SpotLightObject: IBodyOwner, ILightProvider, IGameObjectRenderer
{
    public class Parameters
    {
        [EditorLink(typeof(ITarget))] public int Target { get; set; }
        [EditorFloat(5, 180)] public float Angle1 { get; set; } = 15;
        [EditorFloat(5, 180)] public float Angle2 { get; set; } = 45;
        [Editor] public RgbaColor Color { get; set; } = RgbaColor.White;
        [EditorFloat(0.1f, 4f, 0.1f)] public float Intensity { get; set; } = 1;
        [EditorFloat(0.1f, 1f, 0.1f)] public float AfterGlow { get; set; } = 0.5f;
    }
    
    private const int GlowFanSegments = 4;
    private const int GlowFanRings = 2;

    public IBody Body { get; }
    public int ZOrder { get; }
    private readonly float _tileSize;
    private readonly Vector2[,] _glowFan = new Vector2[GlowFanRings + 1, GlowFanSegments + 1];
    private SpotLight? _spotLight;
    private float _afterGlow;

    public SpotLightObject(GameObjectsServices services, ObjectCreationParameters<Parameters> parameters)
    {
        var simulation = services.Simulation;
        Body = simulation.CreateBody(this);
        Body.Position = parameters.Position;
        Body.IsKinematic = true;
        Body.Mass = 100000;
        ZOrder = parameters.ZOrder;
        _tileSize = services.GameTemplate.TileSize;
        _afterGlow = parameters.Parameters.AfterGlow;

        parameters.LinkMap.RequestLink<ITarget>(parameters.Parameters.Target, t =>
        {
            var light = t.Position - Body.Position;
            var direction = Vector2.Normalize(light);

            var innerAngle = MathF.Min(parameters.Parameters.Angle1, parameters.Parameters.Angle2);
            var outerAngle = MathF.Max(parameters.Parameters.Angle1, parameters.Parameters.Angle2);

            _spotLight = new SpotLight(
                new Vector3(Body.Position, -10) * services.GameTemplate.TileSize,
                direction, outerAngle, innerAngle,
                light.Length() * services.GameTemplate.TileSize,
                parameters.Parameters.Color,
                parameters.Parameters.Intensity);
        });
    }

    public virtual void FillLights(ILightsContainer lightsContainer)
    {
        if (_spotLight is { } light)
        {
            lightsContainer.AddSpotLight(light);
        }
    }

    void IBodyOwner.OnFixedUpdate(out bool cancelUpdate)
    {
        cancelUpdate = false;
        OnFixedUpdate(Body.Simulation.SimulationParameters.TimeDelta);
    }

    protected virtual void OnFixedUpdate(float dt)
    {
    }

    public RectangleF Bounds
    {
        get
        {
            if (_spotLight is not { } light)
            {
                return new RectangleF(Body.Position.X, Body.Position.Y, 0, 0);
            }

            var apex = UpdateGlowArc(light);

            var minX = apex.X;
            var minY = apex.Y;
            var maxX = apex.X;
            var maxY = apex.Y;

            for (var i = 0; i <= GlowFanSegments; i++)
            {
                var point = _glowFan[GlowFanRings, i];
                minX = MathF.Min(minX, point.X);
                minY = MathF.Min(minY, point.Y);
                maxX = MathF.Max(maxX, point.X);
                maxY = MathF.Max(maxY, point.Y);
            }

            minX /= _tileSize; minY /= _tileSize; maxX /= _tileSize; maxY /= _tileSize;

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
    }

    public virtual void Render(IRenderer renderer, RgbaColor color, float depth)
    {
        if (renderer.CurrentPass != RenderPass.Normal || _spotLight is not { } light)
        {
            return;
        }

        UpdateGlowArc(light);

        renderer.StateManager.SaveState();
        renderer.StateManager.SetBlendMode(BlendMode.Additive);
        
        var glowColor = light.Color * (light.Intensity * _afterGlow / 8f);

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

    private Vector2 UpdateGlowArc(SpotLight light)
    {
        var apex = new Vector2(light.Position.X, light.Position.Y);

        var halfAngle = light.OuterAngle * MathF.PI / 180f;
        var baseAngle = MathF.Atan2(light.Direction.Y, light.Direction.X);
        var maxRadius = light.Radius * 0.75f;

        for (var r = 0; r <= GlowFanRings; r++)
        {
            var radius = maxRadius * r / GlowFanRings;

            for (var i = 0; i <= GlowFanSegments; i++)
            {
                var t = (float)i / GlowFanSegments;
                var angle = baseAngle - halfAngle + t * (2f * halfAngle);

                _glowFan[r, i] = apex + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            }
        }

        return apex;
    }

    public void Dispose()
    {
    }
}