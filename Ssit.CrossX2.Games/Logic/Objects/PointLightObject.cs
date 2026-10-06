using System.Numerics;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Lighting;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public class PointLightObject: IBodyOwner, ILightProvider, IGameObjectRenderer
{
    public class Parameters
    {
        [EditorLink(typeof(ITarget), true)] public int Target { get; set; }

        [Editor] public RgbaColor Color { get; set; } = RgbaColor.White;
        [EditorFloat(0.1f, 4f, 0.1f)] public float Intensity { get; set; } = 1;
    }

    private const int GlowCircleSegments = 24;

    public IBody Body { get; }
    public int ZOrder { get; }
    private readonly float _tileSize;
    private readonly Vector2[] _glowCircle = new Vector2[GlowCircleSegments + 1];
    private PointLight? _pointLight;

    public PointLightObject(GameObjectsServices services, ObjectCreationParameters<Parameters> parameters)
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
            var range = t.Position - Body.Position;

            _pointLight = new PointLight(
                new Vector3(Body.Position, 0) * services.GameTemplate.TileSize,
                range.Length() * services.GameTemplate.TileSize,
                parameters.Parameters.Color,
                parameters.Parameters.Intensity);
        });
    }

    public virtual void FillLights(ILightsContainer lightsContainer)
    {
        if (_pointLight is { } light)
        {
            lightsContainer.AddPointLight(light);
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
            if (_pointLight is not { } light)
            {
                return new RectangleF(Body.Position.X, Body.Position.Y, 0, 0);
            }

            var minX = (light.Position.X - light.Radius) / _tileSize;
            var minY = (light.Position.Y - light.Radius) / _tileSize;
            var maxX = (light.Position.X + light.Radius) / _tileSize;
            var maxY = (light.Position.Y + light.Radius) / _tileSize;

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
    }

    public virtual void Render(IRenderer renderer, RgbaColor color, float depth)
    {
        if (renderer.CurrentPass != RenderPass.Glow || _pointLight is not { } light)
        {
            return;
        }

        var center = UpdateGlowCircle(light);

        var glowColor = light.Color * (light.Intensity / 16f);
        var edgeColor = RgbaColor.Transparent;

        var centerVertex = new VertexPct(center, glowColor, Vector2.Zero, depth);

        for (var i = 0; i < _glowCircle.Length - 1; i++)
        {
            renderer.RenderQueue.PushTriangle(
                centerVertex,
                new VertexPct(_glowCircle[i], edgeColor, Vector2.Zero, depth),
                new VertexPct(_glowCircle[i + 1], edgeColor, Vector2.Zero, depth));
        }
    }

    private Vector2 UpdateGlowCircle(PointLight light)
    {
        var center = new Vector2(light.Position.X, light.Position.Y);

        for (var i = 0; i < _glowCircle.Length; i++)
        {
            var angle = i / (float)GlowCircleSegments * (2f * MathF.PI);

            _glowCircle[i] = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * light.Radius;
        }

        return center;
    }

    public void Dispose()
    {
    }
}