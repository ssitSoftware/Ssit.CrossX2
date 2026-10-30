using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class EventToSoundBehavior: SteeringBehavior<ISteeringCharacter>
{
    private readonly Dictionary<string, string> _mapping = new();

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
            obj.SoundContainer.Play(snd, obj.SteeringParameters.GroundMaterial);
        }
        return false;
    }
}