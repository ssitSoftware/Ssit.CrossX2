using Ssit.CrossX2.Framework.Games.Logic;

namespace Ssit.CrossX2.Framework.Games.Rendering.Map;

public class MapDisplayElement: IDisposable
{
    public IReadOnlyList<LayerDisplayElement> Layers => _layers;
    private readonly List<LayerDisplayElement> _layers;
    
    public RgbaColor BackgroundColor { get; }
    public ILightsProvider LightsProvider { get; }
    public RgbaColor AmbientLightColor { get; set; }

    internal MapDisplayElement(List<LayerDisplayElement> layers, RgbaColor backgroundColor, ILightsProvider lightsProvider)
    {
        _layers = layers;
        BackgroundColor = backgroundColor;
        LightsProvider = lightsProvider;
    }

    public void Update(float dt)
    {
        foreach (var layer in _layers)
        {
            layer.Update(dt);
        }
    }
    
    public void Dispose()
    {
        foreach (var layer in Layers)
        {
            layer.Dispose();
        }
        _layers.Clear();
    }
}