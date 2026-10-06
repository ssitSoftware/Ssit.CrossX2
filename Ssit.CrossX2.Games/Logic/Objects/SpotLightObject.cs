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
    }
    
    private const int GlowFanSegments = 16;

    public IBody Body { get; }
    public int ZOrder { get; }
    private readonly float _tileSize;
    private readonly Vector2[] _glowArc = new Vector2[GlowFanSegments + 1];
    private SpotLight? _spotLight;

    public SpotLightObject(GameObjectsServices services, ObjectCreationParameters<Parameters> parameters)
    {
        var simulation = services.Simulation;
        Body = simulation.CreateBody(this);
        Body.Position = parameters.Position;
        Body.IsKinematic = true;
        Body.Mass = 100000;
        ZOrder = parameters.ZOrder;
        _tileSize = services.GameTemplate.TileSize;

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

            foreach (var point in _glowArc)
            {
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
        if (renderer.CurrentPass != RenderPass.Glow || _spotLight is not { } light)
        {
            return;
        }

        var apex = UpdateGlowArc(light);

        var glowColor = light.Color * (light.Intensity / 16f);
        var edgeColor = RgbaColor.Transparent;

        var apexVertex = new VertexPct(apex, glowColor, Vector2.Zero, depth);

        for (var i = 0; i < _glowArc.Length - 1; i++)
        {
            renderer.RenderQueue.PushTriangle(
                apexVertex,
                new VertexPct(_glowArc[i], edgeColor, Vector2.Zero, depth),
                new VertexPct(_glowArc[i + 1], edgeColor, Vector2.Zero, depth));
        }
    }

    private Vector2 UpdateGlowArc(SpotLight light)
    {
        var apex = new Vector2(light.Position.X, light.Position.Y);

        var halfAngle = light.OuterAngle * MathF.PI / 180f;
        var baseAngle = MathF.Atan2(light.Direction.Y, light.Direction.X);

        for (var i = 0; i < _glowArc.Length; i++)
        {
            var t = (float)i / GlowFanSegments;
            var angle = baseAngle - halfAngle + t * (2f * halfAngle);

            _glowArc[i] = apex + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * light.Radius * 0.75f;
        }

        return apex;
    }

    public void Dispose()
    {
    }
}