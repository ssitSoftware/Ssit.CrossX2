using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Lighting;

namespace Ssit.CrossX2.Framework.Games.Rendering;

public class MapLightsContainer : ILightsContainer, IComparer<PointLight>, IComparer<SpotLight>
{
    private RgbaColor _globalAmbient;
    private RgbaColor _alternativeAmbient;

    private readonly List<PointLight> _pointLights = new();
    private readonly List<SpotLight> _spotLights = new();
    private readonly List<GlobalLight> _globalLights = new();
    private Vector2 _lookAt;

    public void ApplyLights(IRenderer renderer, bool useGlobalAmbient, bool useGlobalLights)
    {
        if (renderer.CurrentPass == RenderPass.Glow)
            return;

        renderer.LightingManager.EnableLighting(true);

        renderer.LightingManager.SetGlobalLights(_globalLights);
        renderer.LightingManager.SetAmbientLight(useGlobalAmbient ? _globalAmbient : _alternativeAmbient);

        if (!useGlobalLights)
        {
            renderer.LightingManager.SetPointLights(Array.Empty<PointLight>());
            renderer.LightingManager.SetSpotLights(Array.Empty<SpotLight>());
            return;
        }
        
        renderer.LightingManager.SetResolution(1);
        renderer.LightingManager.SetPointLights(_pointLights);
        renderer.LightingManager.SetSpotLights(_spotLights);
    }

    public void Reset()
    {
        _pointLights.Clear();
        _spotLights.Clear();
        _globalLights.Clear();
    }

    /// <param name="visibleBounds">Visible area of the main layer in world pixels.</param>
    public void Apply(RectangleF visibleBounds)
    {
        _lookAt = visibleBounds.Center;

        // Lights that cannot reach the visible area only cost shader time.
        // (Plain loops instead of RemoveAll so culling allocates nothing per frame.)
        for (var idx = _pointLights.Count - 1; idx >= 0; --idx)
        {
            if (!IsVisible(visibleBounds, _pointLights[idx].Position, _pointLights[idx].Radius))
                _pointLights.RemoveAt(idx);
        }

        for (var idx = _spotLights.Count - 1; idx >= 0; --idx)
        {
            if (!IsVisible(visibleBounds, _spotLights[idx].Position, _spotLights[idx].Radius))
                _spotLights.RemoveAt(idx);
        }

        if (_pointLights.Count > ILightingManager.MaxPointLights)
        {
            _pointLights.Sort(this);
            _pointLights.RemoveRange(ILightingManager.MaxPointLights, _pointLights.Count - ILightingManager.MaxPointLights);
        }

        if (_spotLights.Count > ILightingManager.MaxSpotLights)
        {
            _spotLights.Sort(this);
            _spotLights.RemoveRange(ILightingManager.MaxSpotLights, _spotLights.Count - ILightingManager.MaxSpotLights);
        }
    }

    /// <summary>Conservative test: the light circle (a spot cone always fits in it) touches the rectangle.</summary>
    private static bool IsVisible(RectangleF bounds, Vector3 position, float radius)
    {
        var dx = MathF.Max(MathF.Max(bounds.X - position.X, position.X - bounds.Right), 0f);
        var dy = MathF.Max(MathF.Max(bounds.Y - position.Y, position.Y - bounds.Bottom), 0f);
        return dx * dx + dy * dy < radius * radius;
    }

    public void SetAmbientLight(RgbaColor globalAmbient, RgbaColor? alternativeAmbient)
    {
        _globalAmbient = globalAmbient;
        _alternativeAmbient = alternativeAmbient ?? RgbaColor.White;
    }

    public void AddPointLight(PointLight light) => _pointLights.Add(light);
    public void AddSpotLight(SpotLight light) => _spotLights.Add(light);

    public void AddGlobalLight(GlobalLight light)
    {
        if (_globalLights.Count >= ILightingManager.MaxGlobalLights)
        {
            return;
        }

        _globalLights.Add(light);
    }

    public int Compare(PointLight x, PointLight y)
    {
        var p0 = new Vector2(x.Position.X, x.Position.Y);
        var p1 = new Vector2(y.Position.X, y.Position.Y);
        
        var d0 = (_lookAt - p0).LengthSquared();
        var d1 = (_lookAt - p1).LengthSquared();

        // Nearest first, so RemoveRange drops the farthest lights.
        return d0.CompareTo(d1);
    }

    public int Compare(SpotLight x, SpotLight y)
    {
        var p0 = new Vector2(x.Position.X, x.Position.Y);
        var p1 = new Vector2(y.Position.X, y.Position.Y);
        
        var d0 = (_lookAt - p0).LengthSquared();
        var d1 = (_lookAt - p1).LengthSquared();

        // Nearest first, so RemoveRange drops the farthest lights.
        return d0.CompareTo(d1);
    }
}