namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// SoundParameters struct holds various properties and parameters related to sound effects.
/// It is used in conjunction with sound emitting structures and interfaces to provide detailed control over sound playback characteristics.
/// </summary>
public readonly record struct SoundParameters(float Volume, float Pitch = 1, float Pan = 0)
{
    public SoundParameters() : this(1, 1, 0)
    {
    }

    public SoundParameters Apply(ISoundEmitter emitter, ISoundListener listener)
    {
        var pan = Pan;
        var volume = 1f;

        if (emitter != null && listener != null)
        {
            var offset = emitter.Position - listener.Position;
            volume *= MathF.Pow(MathF.Max(0, MathF.Min(1, 1 - offset.Length() / listener.MaxDistance)), 2);
            
            pan = MathF.Max(-1, MathF.Min(1, offset.X / listener.PanDistance));
            pan *= 0.75f;
        }
        
        return new SoundParameters
        {
            Volume = volume * Volume,
            Pan = pan,
            Pitch = Pitch
        };
    }
}