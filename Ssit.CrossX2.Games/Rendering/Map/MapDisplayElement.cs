namespace Ssit.CrossX2.Framework.Games.Rendering.Map;

public class MapDisplayElement: IDisposable
{
    public IReadOnlyList<LayerDisplayElement> Layers => _layers;
    private readonly List<LayerDisplayElement> _layers;
    
    public RgbaColor BackgroundColor { get; }
    
    internal MapDisplayElement(List<LayerDisplayElement> layers, RgbaColor backgroundColor)
    {
        _layers = layers;
        BackgroundColor = backgroundColor;
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