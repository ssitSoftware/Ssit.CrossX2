namespace Ssit.CrossX2.Framework.Graphics.Effects;

/// <summary>
/// Colour grading applied by the sharp scale effect (the final upscale when CRT simulation is off).
/// Same maths as the colour grading in the CRT simulation, so the two can be matched.
/// Register an instance in the IoC container to set it; defaults leave colours unchanged.
/// </summary>
public class SharpScaleEffectParameters
{
    /// <summary>Exponent applied to each colour channel (1 = unchanged).</summary>
    public float Gamma { get; set; } = 1f;

    /// <summary>Multiplier applied after gamma (1 = unchanged).</summary>
    public float LightnessMultiplier { get; set; } = 1f;

    /// <summary>0 = grayscale, 1 = original color saturation.</summary>
    public float Saturation { get; set; } = 1f;

    /// <summary>True when the parameters leave colours unchanged, so the shader can skip grading.</summary>
    internal bool IsIdentity => Gamma == 1f && LightnessMultiplier == 1f && Saturation == 1f;
}
