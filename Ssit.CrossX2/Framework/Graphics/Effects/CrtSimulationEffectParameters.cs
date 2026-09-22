namespace Ssit.CrossX2.Framework.Graphics.Effects;

public class CrtSimulationEffectParameters
{
    public float BarrelDistortion { get; set; } = 0.02f;
    public float RgbDisplacement { get; set; } = 0.25f;
    public float ScanlineIntensity { get; set; } = 0.2f;
    public float Vignette { get; set; } = 0.25f;
    public float RestoreLightness { get; set; } = 1.2f;
    public float BleedFactor { get; set; } = 1f;
}