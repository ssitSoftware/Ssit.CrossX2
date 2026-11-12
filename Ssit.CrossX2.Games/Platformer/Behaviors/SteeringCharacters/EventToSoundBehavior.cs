using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Graphics.Sprites;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Plays sounds in response to sprite animation events (e.g. footsteps on specific frames).
/// Mapping is event name -> sound name. A sound entry may contain several comma-separated names, one is chosen at random.
/// Pitch is randomized between <see cref="MinPitch"/> and <see cref="MaxPitch"/>.
/// </summary>
public class EventToSoundBehavior: SteeringBehavior<ISteeringCharacter>
{
    private readonly Dictionary<string, string> _mapping = new();

    public float MinPitch { get; set; } = 1f;
    public float MaxPitch { get; set; } = 1f;
    
    // Each event plays the sound with the same name
    public EventToSoundBehavior(params string[] names)
    {
        foreach (var name in names)
        {
            _mapping[name] = name;
        }
    }
    
    // Explicit event name -> sound name mapping
    public EventToSoundBehavior(params (string name, string sound)[] events)
    {
        foreach (var evnt in events)
        {
            _mapping[evnt.name] = evnt.sound;
        }
    }
    
    protected override bool OnEvent(ISteeringCharacter obj, ISpriteEvent @event)
    {
        if (_mapping.TryGetValue(@event.EventName, out var snd))
        {
            // Several variants: pick one at random
            if (snd.Contains(','))
            {
                var snds = snd.Split(',');
                snd = snds[MersenneTwister.Shared.Next(snds.Length)];
            }
            var pitch = MersenneTwister.Shared.NextSingle() * (MaxPitch - MinPitch) + MinPitch;
            // Without a character sound set, fall back to the shared sounds; otherwise let the container choose a variant for the ground material
            if (obj.SoundContainer is null)
            {
                obj.CommonSoundContainer.Play(snd, pitch: pitch, emitter: obj as ISoundEmitter);
            }
            else
            {
                obj.SoundContainer.Play(snd, obj.SteeringParameters.GroundMaterial, pitch: pitch);
            }
        }
        return false;
    }
}