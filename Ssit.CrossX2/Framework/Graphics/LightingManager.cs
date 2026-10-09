using Ssit.CrossX2.Framework.Graphics.Lighting;

namespace Ssit.CrossX2.Framework.Graphics;

internal class LightingManager(LightingManager.IUpdateLightsHandler handler) : ILightingManager
{
    public RgbaColor AmbientLight { get; private set; }
    
    public int PointLightsCount { get; private set; }
    public int SpotLightsCount { get; private set; }

    public float Resolution { get; private set; } = 0.1f;
    public bool LightingEnabled { get; private set; }
    
    public int GlobalLightsCount { get; private set; }

    public PointLight[] PointLights { get; } = new PointLight[ILightingManager.MaxPointLights];
    public SpotLight[] SpotLights { get; } = new SpotLight[ILightingManager.MaxSpotLights];
    public GlobalLight[] GlobalLights { get; } = new GlobalLight[ILightingManager.MaxGlobalLights];

    public interface IUpdateLightsHandler
    {
        void Flush();
    }

    public void EnableLighting(bool enable)
    {
        handler.Flush();
        LightingEnabled = enable;
    }

    public void SetResolution(float resolution)
    {
        handler.Flush();
        Resolution = resolution;
    }

    public void SetAmbientLight(RgbaColor color)
    {
        handler.Flush();
        AmbientLight = color;
    }

    public void SetPointLights(ReadOnlySpan<PointLight> lights)
    {
        handler.Flush();

        if (lights.Length >= ILightingManager.MaxPointLights)
        {
            throw new InvalidOperationException("Max point lights reached");
        }
        
        PointLightsCount = lights.Length;
        for (var idx = 0; idx < lights.Length; ++idx)
        {
            PointLights[idx] = lights[idx];
        }
    }
    
    public void SetPointLights(IReadOnlyList<PointLight> lights)
    {
        handler.Flush();
        
        if (lights.Count >= ILightingManager.MaxPointLights)
        {
            throw new InvalidOperationException("Max point lights reached");
        }
        
        PointLightsCount = lights.Count;
        for (var idx = 0; idx < lights.Count; ++idx)
        {
            PointLights[idx] = lights[idx];
        }
    }

    public void SetSpotLights(ReadOnlySpan<SpotLight> lights)
    {
        handler.Flush();
        
        if (lights.Length >= ILightingManager.MaxSpotLights)
        {
            throw new InvalidOperationException("Max spot lights reached");
        }

        SpotLightsCount = lights.Length;
        for (var idx = 0; idx < lights.Length; ++idx)
        {
            SpotLights[idx] = lights[idx];
        }
    }

    public void SetSpotLights(IReadOnlyList<SpotLight> lights)
    {
        if (lights.Count >= ILightingManager.MaxSpotLights)
        {
            throw new InvalidOperationException("Max spot lights reached");
        }

        SpotLightsCount = lights.Count;
        for (var idx = 0; idx < lights.Count; ++idx)
        {
            SpotLights[idx] = lights[idx];
        }
        handler.Flush();
    }

    public void SetGlobalLights(ReadOnlySpan<GlobalLight> lights)
    {
        handler.Flush();

        if (lights.Length > ILightingManager.MaxGlobalLights)
        {
            throw new InvalidOperationException("Max global lights reached");
        }

        GlobalLightsCount = lights.Length;
        for (var idx = 0; idx < lights.Length; ++idx)
        {
            GlobalLights[idx] = lights[idx];
        }

    }

    public void SetGlobalLights(IReadOnlyList<GlobalLight> lights)
    {
        handler.Flush();

        if (lights.Count > ILightingManager.MaxGlobalLights)
        {
            throw new InvalidOperationException("Max global lights reached");
        }

        GlobalLightsCount = lights.Count;
        for (var idx = 0; idx < lights.Count; ++idx)
        {
            GlobalLights[idx] = lights[idx];
        }

    }
}