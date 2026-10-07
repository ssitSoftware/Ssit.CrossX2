using Ssit.CrossX2.Framework.Graphics.Lighting;

namespace Ssit.CrossX2.Framework.Games.Rendering;

public interface ILightsContainer: ILightsProvider
{
    void SetAmbientLight(RgbaColor globalAmbient, RgbaColor? alternativeAmbient);
    void AddPointLight(PointLight pointLight);
    void AddSpotLight(SpotLight spotLight);
    void SetLighting(RgbaColor white, float intensity);
}

public interface ILightProvider
{
    void FillLights(ILightsContainer lightsContainer);
}