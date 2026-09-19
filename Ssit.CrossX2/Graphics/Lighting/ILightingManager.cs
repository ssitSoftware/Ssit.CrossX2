namespace Ssit.CrossX2.Graphics.Lighting;

public interface ILightingManager
{
    public const int MaxPointLights = 8;
    public const int MaxSpotLights = 8;

    void EnableLighting(bool enable);
    void SetResolution(float resolution);
    void SetAmbientLight(RgbaColor color);
    void SetPointLights(ReadOnlySpan<PointLight> lights);
    void SetSpotLights(ReadOnlySpan<SpotLight> lights);
    void SetPointLights(IReadOnlyList<PointLight> lights);
    void SetSpotLights(IReadOnlyList<SpotLight> lights);
    void SetCellShades(bool global, int shades);
}