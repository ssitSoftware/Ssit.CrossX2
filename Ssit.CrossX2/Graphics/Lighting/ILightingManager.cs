namespace Ssit.CrossX2.Graphics.Lighting;

public interface ILightingManager
{
    public const int MaxPointLights = 8;
    public const int MaxSpotLights = 8;

    void EnableLighting(bool enable);
    void SetResolution(float resolution);
    void SetAmbientLight(RgbaColor color);
    void SetPointLights(ReadOnlySpan<PointLight2D> lights);
    void SetSpotLights(ReadOnlySpan<SpotLight2D> lights);
    void SetPointLights(IReadOnlyList<PointLight2D> lights);
    void SetSpotLights(IReadOnlyList<SpotLight2D> lights);
}