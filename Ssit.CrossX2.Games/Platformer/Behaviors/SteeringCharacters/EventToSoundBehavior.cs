using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Graphics.Sprites;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class EventToSoundBehavior: SteeringBehavior<ISteeringCharacter>
{
    private readonly Dictionary<string, string> _mapping = new();

    public float MinPitch { get; set; } = 1f;
    public float MaxPitch { get; set; } = 1f;
    
    public EventToSoundBehavior(params string[] names)
    {
        foreach (var name in names)
        {
            _mapping[name] = name;
        }
    }
    
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
            if (snd.Contains(','))
            {
                var snds = snd.Split(',');
                snd = snds[MersenneTwister.Shared.Next(snds.Length)];
            }
            var pitch = MersenneTwister.Shared.NextSingle() * (MaxPitch - MinPitch) + MinPitch;
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