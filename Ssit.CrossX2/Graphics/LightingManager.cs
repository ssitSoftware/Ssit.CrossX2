using Ssit.CrossX2.Graphics.Lighting;

namespace Ssit.CrossX2.Graphics;

internal class LightingManager(LightingManager.IUpdateLightsHandler handler) : ILightingManager
{
    public RgbaColor AmbientLight { get; private set; }
    
    public int PointLightsCount { get; private set; }
    public int SpotLightsCount { get; private set; }

    public float Resolution { get; private set; } = 0.1f;
    public bool LightingEnabled { get; private set; }
    
    public PointLight2D[] PointLights { get; } = new PointLight2D[ILightingManager.MaxPointLights];
    public SpotLight2D[] SpotLights { get; } = new SpotLight2D[ILightingManager.MaxSpotLights];
    
    public interface IUpdateLightsHandler
    {
        void OnLightsUpdated();
    }

    public void EnableLighting(bool enable)
    {
        LightingEnabled = enable;
        handler.OnLightsUpdated();
    }

    public void SetResolution(float resolution)
    {
        Resolution = resolution;
        handler.OnLightsUpdated();
    }

    public void SetAmbientLight(RgbaColor color)
    {
        AmbientLight = color;
        handler.OnLightsUpdated();
    }

    public void SetPointLights(ReadOnlySpan<PointLight2D> lights)
    {
        if (lights.Length >= ILightingManager.MaxPointLights)
        {
            throw new InvalidOperationException("Max point lights reached");
        }
        
        PointLightsCount = lights.Length;
        for (var idx = 0; idx < lights.Length; ++idx)
        {
            PointLights[idx] = lights[idx];
        }
        handler.OnLightsUpdated();
    }
    
    public void SetPointLights(IReadOnlyList<PointLight2D> lights)
    {
        if (lights.Count >= ILightingManager.MaxPointLights)
        {
            throw new InvalidOperationException("Max point lights reached");
        }
        
        PointLightsCount = lights.Count;
        for (var idx = 0; idx < lights.Count; ++idx)
        {
            PointLights[idx] = lights[idx];
        }
        handler.OnLightsUpdated();
    }

    public void SetSpotLights(ReadOnlySpan<SpotLight2D> lights)
    {
        if (lights.Length >= ILightingManager.MaxSpotLights)
        {
            throw new InvalidOperationException("Max spot lights reached");
        }
        
        PointLightsCount = lights.Length;
        for (var idx = 0; idx < lights.Length; ++idx)
        {
            SpotLights[idx] = lights[idx];
        }
        handler.OnLightsUpdated();
    }
    
    public void SetSpotLights(IReadOnlyList<SpotLight2D> lights)
    {
        if (lights.Count >= ILightingManager.MaxSpotLights)
        {
            throw new InvalidOperationException("Max spot lights reached");
        }
        
        PointLightsCount = lights.Count;
        for (var idx = 0; idx < lights.Count; ++idx)
        {
            SpotLights[idx] = lights[idx];
        }
        handler.OnLightsUpdated();
    }
}