using Ssit.CrossX2.Framework.Games.Logic.Checkpoints;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Saves a checkpoint when the <paramref name="controlName"/> button is held for <paramref name="holdTime"/> seconds.
/// The character must implement <see cref="ICheckpointCreator"/>; nothing is saved while
/// <see cref="ICheckpointCreator.CanCreateCheckpoint"/> is false. The button has to be released before the next save.
/// After half of the hold time the character enters <paramref name="creatingState"/>; it returns to
/// <paramref name="returnState"/> when the button is released, saving becomes impossible or the checkpoint is saved.
/// Add the behavior (first) to the states the save can start from and to <paramref name="creatingState"/>.
/// The hold timer is stored per object, so it keeps running across these states.
/// </summary>
public class CreateCheckpointBehavior(float holdTime = 1f, string controlName = SteeringControlNames.Operate,
    string creatingState = "CreateCheckpoint", string returnState = "Idle")
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

        var button = obj.SteeringInput.Button(controlName);
        
        if (!button.IsDown)
        {
            parameters.HeldTime = 0;
            parameters.Created = false;
            return LeaveCreatingState(obj, creator);
        }

        if (parameters.Created)
            return LeaveCreatingState(obj, creator);

        // At an existing checkpoint a single press saves right away.
        if (button == ButtonState.JustPressed && creator.IsAtCheckpoint && creator.CanCreateCheckpoint)
        {
            parameters.Created = true;
            creator.CreateCheckpoint();
            return false;
        }
        
        parameters.HeldTime += dt;

        if (!creator.CanCreateCheckpoint)
            return LeaveCreatingState(obj, creator);

        if (parameters.HeldTime < holdTime)
        {
            if (parameters.HeldTime > holdTime / 2 && !IsInCreatingState(obj))
            {
                obj.SetSteeringState(creatingState);
                creator.BeginCreateCheckpoint();
                return true;
            }
            
            return false;
        }

        parameters.Created = true;
        
        creator.CreateCheckpoint();
        
        return LeaveCreatingState(obj, creator, cancelled: false);
    }

    private bool IsInCreatingState(ISteeringCharacter obj) => obj.CurrentSteeringState?.Name == creatingState;

    private bool LeaveCreatingState(ISteeringCharacter obj, ICheckpointCreator creator, bool cancelled = true)
    {
        if (!IsInCreatingState(obj))
            return false;

        if (cancelled)
        {
            creator.CancelCreateCheckpoint();
        }
        
        obj.SetSteeringState(returnState);
        return true;
    }
}
