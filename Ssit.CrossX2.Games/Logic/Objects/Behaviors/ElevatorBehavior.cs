using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Behaviors;

public class ElevatorBehavior : Behavior<Elevator>
{
    public class Parameters
    {
        public Vector2 LastMoveDirection;
        public Vector2 LastTargetPoint = new(-10000);
        public float SpeedFactor;
    }

    protected override void OnEnterState(Elevator elevator)
    {
        elevator.Sprite.Advance(DateTime.Now.Millisecond % 100 / 1000f);
    }

    protected override bool OnUpdate(Elevator elevator, float dt)
    {
        var parameters = elevator.GetParameters<Parameters>();

        if (!elevator.IsOn && elevator.CurrentState != "Off")
        {
            elevator.SetState("Off");
        }

        if (elevator.IsOn && elevator.CurrentState != "On")
        {
            elevator.SetState("On");
        }

        parameters.SpeedFactor += dt * (elevator.IsOn ? 1 : -1) * 2;
        parameters.SpeedFactor = MathF.Min(MathF.Max(parameters.SpeedFactor, 0), 1);

        while (true)
        {
            var diff = elevator.CurrentTarget.Position - elevator.Body.Position;

            var moveDir = diff;
            if (diff.Length() > 0)
            {
                moveDir = Vector2.Normalize(diff);
            }

            if (parameters.LastMoveDirection.Length() != 0 && (Vector2.Dot(moveDir, parameters.LastMoveDirection) < 0 || moveDir.Length() < 0.01f))
            {
                parameters.LastTargetPoint = elevator.CurrentTarget.Position;
                var nextTarget = elevator.CurrentTarget.Next ?? elevator;

                elevator.CurrentTarget = nextTarget;
                parameters.LastMoveDirection = Vector2.Zero;
                continue;
            }

            var speed = elevator.Speed * parameters.SpeedFactor;

            if (elevator.BrakingDistance > 0)
            {
                var factor = diff.Length() / elevator.BrakingDistance + 0.2f;
                var factor2 = (parameters.LastTargetPoint - elevator.Body.Position).Length() / elevator.BrakingDistance + 0.2f;

                factor = MathF.Min(MathF.Min(factor2, factor), 1);
                factor = MathF.Sin(factor * MathF.PI / 2);
                speed *= factor;

                speed = MathF.Max(speed, 0.01f);
            }

            speed = MathF.Min(diff.Length() / dt, speed);

            var velocity = speed * moveDir;
            var move = velocity * dt;

            elevator.Body.KinematicMove(move, KinematicMoveMode.Kinematic);
            parameters.LastMoveDirection = moveDir;
            break;
        }

        return true;
    }
}
