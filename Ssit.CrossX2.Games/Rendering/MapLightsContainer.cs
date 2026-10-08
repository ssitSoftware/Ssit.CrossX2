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
    private Vector2 _lookAt;

    private RgbaColor _lightingColor = RgbaColor.White;
    private float _lightIntensity = 0;

    private readonly DirectionalLight[] _lightingLight = [new(Vector3.One, RgbaColor.White, 0f)];
    
    public void ApplyLights(IRenderer renderer, bool useGlobalAmbient, bool useGlobalLights)
    {
        if (renderer.CurrentPass == RenderPass.Glow)
            return;
        
        renderer.LightingManager.EnableLighting(true);
        
        _lightingLight[0] = new DirectionalLight(new Vector3(0, 0, -1), _lightingColor, _lightIntensity);
        
        renderer.LightingManager.SetDirectionalLights(_lightingLight);
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
    }

    public void Apply(Vector2 lookAt)
    {
        _lookAt = lookAt;

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

    public void SetAmbientLight(RgbaColor globalAmbient, RgbaColor? alternativeAmbient)
    {
        _globalAmbient = globalAmbient;
        _alternativeAmbient = alternativeAmbient ?? RgbaColor.White;
    }

    public void AddPointLight(PointLight light) => _pointLights.Add(light);
    public void AddSpotLight(SpotLight light) => _spotLights.Add(light);

    public void SetLighting(RgbaColor color, float intensity)
    {
        _lightingColor = color;
        _lightIntensity = intensity;
    }

    public int Compare(PointLight x, PointLight y)
    {
        var p0 = new Vector2(x.Position.X, x.Position.Y);
        var p1 = new Vector2(y.Position.X, y.Position.Y);
        
        var d0 = (_lookAt - p0).Length();
        var d1 = (_lookAt - p1).Length();

        return Math.Sign(d1 - d0);
    }

    public int Compare(SpotLight x, SpotLight y)
    {
        var p0 = new Vector2(x.Position.X, x.Position.Y);
        var p1 = new Vector2(y.Position.X, y.Position.Y);
        
        var d0 = (_lookAt - p0).Length();
        var d1 = (_lookAt - p1).Length();

        return Math.Sign(d1 - d0);
    }
}