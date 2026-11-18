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

    // Every setter flushes the render queue only when the lighting state really changes:
    // a flush ends the current batch (vertex upload + new GPU render pass), and the map
    // renderer re-applies the same lights for every layer.

    public void EnableLighting(bool enable)
    {
        if (LightingEnabled == enable)
            return;

        handler.Flush();
        LightingEnabled = enable;
    }

    public void SetResolution(float resolution)
    {
        if (Resolution.Equals(resolution))
            return;

        handler.Flush();
        Resolution = resolution;
    }

    public void SetAmbientLight(RgbaColor color)
    {
        if (AmbientLight.Equals(color))
            return;

        handler.Flush();
        AmbientLight = color;
    }

    public void SetPointLights(ReadOnlySpan<PointLight> lights)
    {
        if (lights.Length > ILightingManager.MaxPointLights)
        {
            throw new InvalidOperationException("Max point lights reached");
        }

        if (PointLightsCount == lights.Length && lights.SequenceEqual(new ReadOnlySpan<PointLight>(PointLights, 0, lights.Length)))
            return;

        handler.Flush();

        PointLightsCount = lights.Length;
        lights.CopyTo(PointLights);
    }
    
    public void SetPointLights(IReadOnlyList<PointLight> lights)
    {
        if (lights.Count > ILightingManager.MaxPointLights)
        {
            throw new InvalidOperationException("Max point lights reached");
        }

        if (PointLightsCount == lights.Count && SameAs(PointLights, lights))
            return;

        handler.Flush();
        
        PointLightsCount = lights.Count;
        for (var idx = 0; idx < lights.Count; ++idx)
        {
            PointLights[idx] = lights[idx];
        }
    }

    public void SetSpotLights(ReadOnlySpan<SpotLight> lights)
    {
        if (lights.Length > ILightingManager.MaxSpotLights)
        {
            throw new InvalidOperationException("Max spot lights reached");
        }

        if (SpotLightsCount == lights.Length && lights.SequenceEqual(new ReadOnlySpan<SpotLight>(SpotLights, 0, lights.Length)))
            return;

        handler.Flush();

        SpotLightsCount = lights.Length;
        lights.CopyTo(SpotLights);
    }

    public void SetSpotLights(IReadOnlyList<SpotLight> lights)
    {
        if (lights.Count > ILightingManager.MaxSpotLights)
        {
            throw new InvalidOperationException("Max spot lights reached");
        }

        if (SpotLightsCount == lights.Count && SameAs(SpotLights, lights))
            return;

        // Flush before changing the lights, so already queued draws keep the lights they were issued with.
        handler.Flush();

        SpotLightsCount = lights.Count;
        for (var idx = 0; idx < lights.Count; ++idx)
        {
            SpotLights[idx] = lights[idx];
        }
    }

    public void SetGlobalLights(ReadOnlySpan<GlobalLight> lights)
    {
        if (lights.Length > ILightingManager.MaxGlobalLights)
        {
            throw new InvalidOperationException("Max global lights reached");
        }

        if (GlobalLightsCount == lights.Length && lights.SequenceEqual(new ReadOnlySpan<GlobalLight>(GlobalLights, 0, lights.Length)))
            return;

        handler.Flush();

        GlobalLightsCount = lights.Length;
        lights.CopyTo(GlobalLights);
    }

    public void SetGlobalLights(IReadOnlyList<GlobalLight> lights)
    {
        if (lights.Count > ILightingManager.MaxGlobalLights)
        {
            throw new InvalidOperationException("Max global lights reached");
        }

        if (GlobalLightsCount == lights.Count && SameAs(GlobalLights, lights))
            return;

        handler.Flush();

        GlobalLightsCount = lights.Count;
        for (var idx = 0; idx < lights.Count; ++idx)
        {
            GlobalLights[idx] = lights[idx];
        }
    }

    private static bool SameAs<T>(T[] current, IReadOnlyList<T> next) where T : struct, IEquatable<T>
    {
        for (var idx = 0; idx < next.Count; ++idx)
        {
            if (!current[idx].Equals(next[idx]))
                return false;
        }

        return true;
    }
}
