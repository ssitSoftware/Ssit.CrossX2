using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

public class HorizontalPatrolBehavior(bool returnToOriginalPositionIfNotVisible) : SteeringBehavior<ISteeringCharacter>
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Parameters
    {
        internal bool MoveTowardsTarget { get; set; } = true;
        internal bool WasInitialized { get; set; }
        public ITarget Target{ get; set; }
        public bool WasDisplayed { get; set; }
        public Vector2 InitialPosition { get; set; }
        public float Speed { get; set; } = 10f;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<Parameters>(true);
        parameters.WasInitialized |= parameters.WasDisplayed;
        
        if (parameters.Target is null || !parameters.WasInitialized)
            return false;
        
        bool moveTowardsTarget = parameters.MoveTowardsTarget;

        if (!parameters.WasDisplayed && returnToOriginalPositionIfNotVisible)
        {
            moveTowardsTarget = false;
        }
        
        var destination = moveTowardsTarget ? parameters.Target.Position.X : parameters.InitialPosition.X;

        if (MathF.Abs(obj.Body.Position.X - destination) < 0.1f)
        {
            parameters.MoveTowardsTarget = !moveTowardsTarget;
            destination = parameters.MoveTowardsTarget ? parameters.Target.Position.X : parameters.InitialPosition.X;
        }

        var velocity = MathF.Sign(destination - obj.Body.Position.X) * parameters.Speed *
                       obj.Body.Simulation.SimulationParameters.TimeDelta;

        velocity = MathF.Min(velocity, MathF.Abs(destination - obj.Body.Position.X));

        obj.FaceLeft = velocity < 0;
        obj.Body.KinematicMove(new Vector2(velocity, 0), KinematicMoveMode.Kinematic);

        obj.SetSteeringState("Move");
        return false;
    }
}
