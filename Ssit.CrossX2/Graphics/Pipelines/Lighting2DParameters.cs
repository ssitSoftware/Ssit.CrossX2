namespace CrossX2.Graphics.Pipelines;

public class Lighting2DParameters
{
    public const int MaxLights = 8;
    public const int MaxSpotLights = 4;

    public RgbaColor Ambient { get; set; }

    public IReadOnlyList<PointLight2D> Lights
    {
        get => field;
        set
        {
            if (value.Count > MaxLights) throw new InvalidOperationException("Max lights reached");
            field = value;
        }
    } = [];

    public IReadOnlyList<SpotLight2D> SpotLights
    {
        get => field;
        set
        {
            if (value.Count > MaxSpotLights) throw new InvalidOperationException("Max spot lights reached");
            field = value;
        }
    } = [];

    public float Resolution { get; set; }
}
