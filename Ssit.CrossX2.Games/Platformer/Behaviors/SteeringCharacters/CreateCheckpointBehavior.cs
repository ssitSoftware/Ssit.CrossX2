using Ssit.CrossX2.Framework.Games.Logic.Checkpoints;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Saves a checkpoint when the <paramref name="controlName"/> button is held for <paramref name="holdTime"/> seconds.
/// The character must implement <see cref="ICheckpointCreator"/>; nothing is saved while
/// <see cref="ICheckpointCreator.CanCreateCheckpoint"/> is false. The button has to be released before the next save.
/// The hold timer is stored per object, so it keeps running when the character moves between states that have this behavior.
/// Place it first in the state, so behaviors that stop the state's update can't skip it.
/// </summary>
public class CreateCheckpointBehavior(float holdTime = 1f, string controlName = SteeringControlNames.Save)
    : SteeringBehavior<ISteeringCharacter>
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class Parameters
    {
        // Seconds the button has been held
        public float HeldTime;
        
        // A checkpoint was saved during the current press
        public bool Created;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj is not ICheckpointCreator creator)
            return false;
        
        var parameters = obj.GetParameters<Parameters>();

        if (!obj.SteeringInput.Button(controlName).IsDown)
        {
            parameters.HeldTime = 0;
            parameters.Created = false;
            return false;
        }

        if (parameters.Created)
            return false;
        
        parameters.HeldTime += dt;

        if (parameters.HeldTime < holdTime || !creator.CanCreateCheckpoint)
            return false;

        parameters.Created = true;
        
        creator.CheckpointService.Save(0);
        creator.OnCheckpointCreated();
        
        return false;
    }
}
