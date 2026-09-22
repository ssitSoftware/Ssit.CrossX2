using Ssit.CrossX2.Framework.Graphics.Lighting;

namespace Ssit.CrossX2.Framework.Graphics;

internal class LightingManager(LightingManager.IUpdateLightsHandler handler) : ILightingManager
{
    public RgbaColor AmbientLight { get; private set; }
    
    public int PointLightsCount { get; private set; }
    public int SpotLightsCount { get; private set; }

    public float Resolution { get; private set; } = 0.1f;
    public bool LightingEnabled { get; private set; }
    public bool BumpMappingEnabled { get; private set; }
    
    public int DirectionalLightsCount { get; private set; }

    public PointLight[] PointLights { get; } = new PointLight[ILightingManager.MaxPointLights];
    public SpotLight[] SpotLights { get; } = new SpotLight[ILightingManager.MaxSpotLights];
    public DirectionalLight[] DirectionalLights { get; } = new DirectionalLight[ILightingManager.MaxDirectionalLights];
    public int LocalCellShades { get; private set; }
    public int GlobalCellShades { get; private set; }

    public interface IUpdateLightsHandler
    {
        void OnLightsUpdated();
    }

    public void EnableLighting(bool enable, bool enableBumpMapping)
    {
        LightingEnabled = enable;
        BumpMappingEnabled = enableBumpMapping & enable;
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

    public void SetPointLights(ReadOnlySpan<PointLight> lights)
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
    
    public void SetPointLights(IReadOnlyList<PointLight> lights)
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

    public void SetSpotLights(ReadOnlySpan<SpotLight> lights)
    {
        if (lights.Length >= ILightingManager.MaxSpotLights)
        {
            throw new InvalidOperationException("Max spot lights reached");
        }

        SpotLightsCount = lights.Length;
        for (var idx = 0; idx < lights.Length; ++idx)
        {
            SpotLights[idx] = lights[idx];
        }
        handler.OnLightsUpdated();
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
        handler.OnLightsUpdated();
    }

    public void SetDirectionalLights(ReadOnlySpan<DirectionalLight> lights)
    {
        if (lights.Length > ILightingManager.MaxDirectionalLights)
        {
            throw new InvalidOperationException("Max directional lights reached");
        }

        DirectionalLightsCount = lights.Length;
        for (var idx = 0; idx < lights.Length; ++idx)
        {
            DirectionalLights[idx] = lights[idx];
        }
        handler.OnLightsUpdated();
    }

    public void SetDirectionalLights(IReadOnlyList<DirectionalLight> lights)
    {
        if (lights.Count > ILightingManager.MaxDirectionalLights)
        {
            throw new InvalidOperationException("Max directional lights reached");
        }

        DirectionalLightsCount = lights.Count;
        for (var idx = 0; idx < lights.Count; ++idx)
        {
            DirectionalLights[idx] = lights[idx];
        }
        handler.OnLightsUpdated();
    }

    public void SetCellShades(bool global, int shades)
    {
        LocalCellShades = global ? 0 : shades;
        GlobalCellShades = global ? shades : 0;
        handler.OnLightsUpdated();
    }
}