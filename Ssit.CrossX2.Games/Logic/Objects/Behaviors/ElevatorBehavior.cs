using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Behaviors;

public class ElevatorBehavior(Elevator elevator): Behavior
{
    private Vector2 _lastMoveDirection;
    private Vector2 _lastTargetPoint = new Vector2(-10000);
    private float _speedFactor;

    protected override void OnEnterState()
    {
        elevator.Sprite.Advance(DateTime.Now.Millisecond % 100 / 1000f);
    }

    protected override bool OnFixedUpdate(float dt)
    {
        if (!elevator.IsOn && elevator.CurrentState != "Off")
        {
            elevator.SetState("Off");
        }
        
        if (elevator.IsOn && elevator.CurrentState != "On")
        {
            elevator.SetState("On");
        }
        
        _speedFactor += dt * (elevator.IsOn ? 1 : -1) * 2;
        _speedFactor = MathF.Min(MathF.Max(_speedFactor, 0), 1);
        
        while (true)
        {
            var diff = elevator.CurrentTarget.Position - elevator.Body.Position;

            var moveDir = diff;
            if (diff.Length() > 0)
            {
                moveDir = Vector2.Normalize(diff);
            }

            if (_lastMoveDirection.Length() != 0 && (Vector2.Dot(moveDir, _lastMoveDirection) < 0 || moveDir.Length() < 0.01f))
            {
                _lastTargetPoint = elevator.CurrentTarget.Position;
                var nextTarget = elevator.CurrentTarget.Next ?? elevator;
                
                elevator.CurrentTarget = nextTarget;
                _lastMoveDirection = Vector2.Zero;
                continue;
            }

            var speed = elevator.Speed * _speedFactor;

            if (elevator.BrakingDistance > 0)
            {
                var factor = diff.Length() / elevator.BrakingDistance + 0.2f;
                var factor2 = (_lastTargetPoint - elevator.Body.Position).Length() / elevator.BrakingDistance + 0.2f;  
                 
                factor = MathF.Min(MathF.Min(factor2, factor), 1);
                factor = MathF.Sin(factor * MathF.PI / 2);
                speed *= factor;
                
                speed = MathF.Max(speed, 0.01f);
            }
            
            speed = MathF.Min(diff.Length() / dt, speed);
            
            var velocity = speed * moveDir;
            var move = velocity * dt;
            
            elevator.Body.KinematicMove(move, KinematicMoveMode.Kinematic);
            _lastMoveDirection = moveDir;
            break;
        }

        return true;
    }
}