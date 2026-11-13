using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Checkpoints;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract partial class Elevator : IStatefulObject<Elevator.State>
{
    public class State
    {
        public Vector2 Position { get; set; }
        
        /// <summary>
        /// Map id of the target the elevator is moving to - a target object or the elevator itself (its start position).
        /// 0 if unknown - the elevator keeps the target it was created with.
        /// </summary>
        public int TargetId { get; set; }
    }

    public State CaptureState() => new()
    {
        Position = Body.Position,
        TargetId = FindTargetId(CurrentTarget)
    };

    /// <summary>
    /// Restores only the position and the current target - everything else stays as created from the map.
    /// </summary>
    public void RestoreState(State state)
    {
        Body.Position = state.Position;
        Body.Touch();

        if (state.TargetId == 0)
            return;

        if (state.TargetId == MapId)
        {
            CurrentTarget = this;
        }
        else if (Services.Container.Get<IMapObjects>().TryGet(state.TargetId, out var entry) && entry.Instance is ITarget target)
        {
            CurrentTarget = target;
        }
    }

    private int FindTargetId(ITarget target)
    {
        if (target is null)
            return 0;

        if (ReferenceEquals(target, this))
            return MapId;

        foreach (var entry in Services.Container.Get<IMapObjects>().All)
        {
            if (ReferenceEquals(entry.Instance, target))
                return entry.MapId;
        }

        return 0;
    }
}
