using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.AabbPhysics;

internal class Body : IBody
{
    private const double InactivityTimeout = 1;

    public float Mass { get; set; }

    public bool IsActive { get; private set; }
    public bool IsKinematic { get; set; }

    Vector2 IBody.Velocity
    {
        get => Velocity;
        set
        {
            var diff = value - Velocity;
            Velocity = value;

            if (diff.LengthSquared() > float.Epsilon)
            {
                Touch();
            }
        }
    }

    Vector2 IBody.Position
    {
        get => Position;
        set
        {
            Position = value;
            Touch();
        }
    }

    internal Vector2 Position
    {
        get => _position;
        set
        {
            _position = value;
            UpdateCollidersInTree();
        }
    }

    private Vector2 Velocity
    {
        get;
        set
        {
            if (value.Y > 50)
            {
                value = new Vector2(value.X, 50);
            }

            field = value;
        }
    }

    public ICollider[] Colliders { get; private set; }

    public IBodyOwner Owner { get; }

    public ISimulation Simulation => _simulation;

    private readonly Simulation _simulation;
    private Vector2 _position;

    private Vector2 _force = Vector2.Zero;
    public Vector2 KinematicVelocity { get; set; } = Vector2.Zero;
    public float StepUpTolerance { get; set; } = 0.1f;
    public int UpdateOrder { get; set; }

    private readonly List<ICollider> _staticCollisions = new();

    private double _timeSinceLastTouch;

    internal Body(Simulation simulation)
    {
        _simulation = simulation;
    }

    internal Body(Simulation simulation, IBodyOwner owner)
    {
        _simulation = simulation;
        Owner = owner;
        
        if (owner is IBodyEventsReceiver rec)
        {
            _eventsReceivers.Add(rec);
        }
    }

    private readonly List<IBodyEventsReceiver> _eventsReceivers = new();

    public void LimitVelocity(float maxVelocity) => LimitVelocity(maxVelocity, maxVelocity);

    public void LimitVelocity(float maxHorizontalVelocity, float maxVerticalVelocity)
    {
        var velocity = Velocity;
        velocity.X = MathF.Sign(velocity.X) * MathF.Min(maxHorizontalVelocity, MathF.Abs(velocity.X));
        velocity.Y = MathF.Sign(velocity.Y) * MathF.Min(maxVerticalVelocity, MathF.Abs(velocity.Y));

        Velocity = velocity;
    }

    public void Touch()
    {
        IsActive = true;
        _timeSinceLastTouch = 0;
    }

    public void ApplyForce(Vector2 force)
    {
        _force += force;
        Touch();
    }


    public void AddColliders(params ICollider[] colliders)
    {
        foreach (var collider in colliders)
        {
            if (collider.AttachedBody != this) throw new InvalidProgramException("Given collider is not attached to this body.");
        }

        Colliders = colliders;
        
        foreach (var collider in Colliders)
        {
            _simulation.UpdateColliderInTree(collider);
        }
    }

    public void KinematicMove(Vector2 move, KinematicMoveMode mode, IBody skipBody = null)
    {
        IsActive = true;

        if (mode is KinematicMoveMode.Move or KinematicMoveMode.MoveAddVelocity)
        {
            MoveInternal(ref move);

            if (mode == KinematicMoveMode.MoveAddVelocity)
            {
                KinematicVelocity += move / Simulation.SimulationParameters.TimeDelta;
            }
            return;
        }
        
        var kinematicStandardMoveHybrid = mode == KinematicMoveMode.Hybrid;
        
        var normal = Vector2.Zero;
        var friction = Vector2.Zero;

        var attemptedMove = move;
        var tries = kinematicStandardMoveHybrid ? 1 : 4;
        var collision = false;
        
        while (tries-- > 0)
        {
            for (var idx = 0; idx < Colliders.Length; ++idx)
            {
                if (!Colliders[idx].IsActive) continue;
                if (MovementCollisionCalculator.GetMovementCollisions(_simulation, ColliderType.Dynamic, this, idx, ref move, ref normal, ref friction, out var horizontalMovementCollider, out var verticalMovementCollider))
                {
                    collision = true;
                    if (horizontalMovementCollider is not null)
                    {
                        if (horizontalMovementCollider.AttachedBody != skipBody)
                        {
                            var modifier = kinematicStandardMoveHybrid ? Mass / (Mass + horizontalMovementCollider.AttachedBody.Mass) : 1;
                            horizontalMovementCollider.AttachedBody?.KinematicMove(new Vector2(attemptedMove.X - move.X, 0) * modifier, mode, this);
                            horizontalMovementCollider.RaiseCollisionWith(false, Colliders[idx], Vector2.Zero);
                            move = attemptedMove;
                            break;
                        }
                    }

                    if (verticalMovementCollider is not null)
                    {
                        if (skipBody != verticalMovementCollider.AttachedBody)
                        {
                            var modifier = kinematicStandardMoveHybrid ? Mass / (Mass + verticalMovementCollider.AttachedBody.Mass) : 1;
                            verticalMovementCollider.AttachedBody?.KinematicMove(new Vector2(0, attemptedMove.Y - move.Y) * modifier, mode, this);
                            verticalMovementCollider.RaiseCollisionWith(false, Colliders[idx], Vector2.Zero);
                            move = attemptedMove;
                            break;
                        }
                    }
                }
                move = attemptedMove;
            }

            if (!collision) break;
        }

        if (kinematicStandardMoveHybrid)
        {
            KinematicVelocity += move / Simulation.SimulationParameters.TimeDelta;
            Move(move);
        }
        else
        {
            Position += move;
            KinematicVelocity += move / Simulation.SimulationParameters.TimeDelta;
            OnMoved(move);
            UpdateCollidersInTree();
        }
    }

    private void OnMoved(Vector2 move)
    {
        foreach (var rec in _eventsReceivers)
        {
            rec.OnBodyMoved(move);
        }
    }

    public void Move(Vector2 offset) => MoveInternal(ref offset);
    
    private void MoveInternal(ref Vector2 offset)
    {
        IsActive = true;

        for (var idx = 0; idx < Colliders.Length; ++idx)
        {
            offset = ComputeOffset(idx, offset);
        }

        for (var idx = 0; idx < Colliders.Length; ++idx)
        {
            CheckTriggers(idx, offset);
        }
        Position += offset;
        OnMoved(offset);
    }

    private void CheckTriggers(int index, Vector2 move)
    {
        var normal = Vector2.Zero;
        var friction = Vector2.Zero;

        MovementCollisionCalculator.GetMovementCollisions(_simulation, ColliderType.Trigger, this, index, ref move, ref normal, ref friction, out var horizontalMovementCollider,
            out var verticalMovementCollider);

        if (verticalMovementCollider != null)
        {
            var verticalColliderBody = verticalMovementCollider.AttachedBody;
            var colliderVelocity = verticalColliderBody?.Velocity ?? Vector2.Zero;
            Colliders[index].RaiseCollisionWith(true, verticalMovementCollider, new Vector2(0, colliderVelocity.Y - Velocity.Y));
            verticalMovementCollider.RaiseCollisionWith(false, Colliders[index], new Vector2(0, Velocity.Y - colliderVelocity.Y));
        }

        if (horizontalMovementCollider != null)
        {
            var horizontalColliderBody = horizontalMovementCollider.AttachedBody;
            var colliderVelocity = horizontalColliderBody?.Velocity ?? Vector2.Zero;

            Colliders[index].RaiseCollisionWith(true, horizontalMovementCollider, new Vector2(colliderVelocity.X - Velocity.X, 0));
            horizontalMovementCollider.RaiseCollisionWith(false, Colliders[index], new Vector2(Velocity.X - colliderVelocity.X, 0));
        }
    }

    private Vector2 ComputeOffset(int index, Vector2 move)
    {
        if (!Colliders[index].IsActive) return move;

        var normal = Vector2.Zero;
        var friction = Vector2.Zero;
        var attemptedMove = move;

        MovementCollisionCalculator.GetMovementCollisions(_simulation, ColliderType.Static | ColliderType.Dynamic, this, index, ref move, ref normal, ref friction, out var horizontalMovementCollider,
            out var verticalMovementCollider);

        if (horizontalMovementCollider == null && verticalMovementCollider == null) return move;

        var oldVelocity = Velocity;

        if (verticalMovementCollider != null)
        {
            var verticalColliderBody = verticalMovementCollider.AttachedBody;
            var colliderVelocity = verticalColliderBody?.Velocity ?? Vector2.Zero;

            Colliders[index].RaiseCollisionWith(true, verticalMovementCollider, new Vector2(0, colliderVelocity.Y - Velocity.Y));
            verticalMovementCollider.RaiseCollisionWith(false, Colliders[index], new Vector2(0, Velocity.Y - colliderVelocity.Y));

            if (verticalColliderBody != null && verticalMovementCollider.Type == ColliderType.Dynamic && !verticalColliderBody.IsKinematic)
            {
                var newVelocity = (Velocity.Y * Mass +
                                   verticalColliderBody.Velocity.Y * verticalColliderBody.Mass) /
                                  (Mass + verticalColliderBody.Mass);
                Velocity = Velocity with { Y = newVelocity };
                verticalColliderBody.Velocity = verticalColliderBody.Velocity with { Y = newVelocity };
                verticalColliderBody.Touch();
                Touch();
            }
            else
            {
                Velocity = Velocity with { Y = 0 };
            }
        }

        if (horizontalMovementCollider != null)
        {
            var horizontalColliderBody = horizontalMovementCollider.AttachedBody;
            var colliderVelocity = horizontalColliderBody?.Velocity ?? Vector2.Zero;

            Colliders[index].RaiseCollisionWith(true, horizontalMovementCollider, new Vector2(colliderVelocity.X - Velocity.X, 0));
            horizontalMovementCollider.RaiseCollisionWith(false, Colliders[index], new Vector2(Velocity.X - colliderVelocity.X, 0));

            var colliderAabb = horizontalMovementCollider.Aabb;
            var myAabb = Colliders[0].Aabb;

            if (StepUpTolerance > 0 && colliderAabb.Top + StepUpTolerance > myAabb.Bottom)
            {
                var offset = MathF.Abs(myAabb.Bottom - colliderAabb.Top) + MovementCollisionCalculator.MovementEpsilon;
                _position.Y -= offset;
                var leftMove = (attemptedMove.X - move.X);

                leftMove = MathF.Sign(leftMove) * (Math.Max(0.01f, (int)(Math.Abs(leftMove) - Math.Sqrt(offset))));

                move += ComputeOffset(index, new Vector2(leftMove, 0));
                return move;
            }

            if (horizontalColliderBody != null && horizontalMovementCollider.Type == ColliderType.Dynamic)
            {
                var newVelocity = (Velocity.X * Mass +
                                   horizontalColliderBody.Velocity.X * horizontalColliderBody.Mass) /
                                  (Mass + horizontalColliderBody.Mass);
                
                Velocity = Velocity with { X = newVelocity };

                horizontalColliderBody.Velocity = horizontalColliderBody.Velocity with { X = newVelocity };
                horizontalColliderBody.Touch();
                Touch();
            }
            else
            {
                Velocity = Velocity with { X = 0 };
            }
        }

        if (Math.Abs(normal.X) > double.Epsilon)
        {
            var velocity = (oldVelocity - Velocity);
            Velocity = Velocity with { X = Math.Abs(velocity.X) * normal.X * Colliders[index].Material.Bounce };
        }

        if (Math.Abs(normal.Y) > double.Epsilon)
        {
            var velocity = (oldVelocity - Velocity);
            Velocity = Velocity with { Y = Math.Abs(velocity.Y) * normal.Y * Colliders[index].Material.Bounce };
        }

        if (attemptedMove.Y * _force.Y > 0)
        {
            friction.X *= MathF.Abs(_force.Y / 1000.0f);
        }
        else
        {
            friction.X = 0;
        }

        if (attemptedMove.X * _force.X > 0)
        {
            friction.Y *= MathF.Abs(_force.X / 1000.0f);
        }
        else
        {
            friction.Y = 0;
        }

        var frictionVelocityX = Velocity.X * Math.Min(1, friction.X * Colliders[index].Material.Friction * Simulation.SimulationParameters.TimeDelta);
        var frictionVelocityY = Velocity.Y * Math.Min(1, friction.Y * Colliders[index].Material.Friction * Simulation.SimulationParameters.TimeDelta);

        if (MathF.Abs(frictionVelocityX) > float.Epsilon || MathF.Abs(frictionVelocityY) > float.Epsilon)
        {
            foreach (var rec in _eventsReceivers)
            {
                rec.OnFriction(ref frictionVelocityX, ref frictionVelocityY);
            }
        }
        
        Velocity -= new Vector2(frictionVelocityX, frictionVelocityY);
        
        if (Math.Abs(Velocity.X) < MovementCollisionCalculator.MovementEpsilon) Velocity = Velocity with { X = 0 };
        if (Math.Abs(Velocity.Y) < MovementCollisionCalculator.MovementEpsilon) Velocity = Velocity with { Y = 0 };

        return move;
    }

    public void PostFixedUpdate()
    {
        _force = Vector2.Zero;
        Owner?.OnPostFixedUpdate();

        foreach (var rec in _eventsReceivers)
        {
            rec.OnBodyUpdated();
        }
    }

    public void FixedUpdate(float dt)
    {
        var beforeUpdatePosition = Position;

        var cancelUpdate = false;
        Owner?.OnFixedUpdate(out cancelUpdate);

        if (cancelUpdate) return;

        if (!IsActive) return;
        if (IsKinematic) return;

        var lastPosition = Position;
        
        _force += Simulation.SimulationParameters.GravityAcceleration * Mass;
        Velocity += _force / Mass * dt;

        var move = Velocity * dt;

        for (var idx = 0; idx < Colliders.Length; ++idx)
        {
            move = ComputeOffset(idx, move);
        }
        
        Position += move;

        _staticCollisions.Clear();

        foreach (var collider in Colliders)
        {
            if (Simulation.CheckCollision(collider.Aabb, this, collider.Material.ColliderGroup, MovementCollisionCalculator.MovementEpsilon, _staticCollisions))
            {
                for (var idx = 0; idx < _staticCollisions.Count; ++idx)
                {
                    FixPosition(collider, _staticCollisions[idx]);
                }
            }
        }

        _staticCollisions.Clear();

        foreach (var collider in Colliders)
        {
            if (Simulation.CheckCollision(collider.Aabb, this, collider.Material.ColliderGroup, MovementCollisionCalculator.MovementEpsilon, _staticCollisions))
            {
                for (var idx = 0; idx < _staticCollisions.Count; ++idx)
                {
                    if (_staticCollisions[idx].Material.Sides == ColliderSides.All)
                    {
                        FixInside(collider, _staticCollisions[idx]);
                    }
                    
                    if (_staticCollisions[idx].Material.Sides == ColliderSides.Top && Velocity.Y >= 0)
                    {
                        FixInsideByStepUp(collider, _staticCollisions[idx]);
                    }
                }
            }
        }
        
        if (lastPosition == Position)
        {
            _timeSinceLastTouch += dt;

            if (_timeSinceLastTouch > InactivityTimeout)
            {
                IsActive = false;
            }
        }
        else
        {
            _timeSinceLastTouch = 0;
            IsActive = true;
        }

        move = Position - beforeUpdatePosition;
        OnMoved(move);
        _force = Vector2.Zero;
        KinematicVelocity = Vector2.Zero;
    }
    
    private void FixInsideByStepUp(ICollider collider, ICollider other)
    {
        var tries = 2;
        while (tries-- > 0 && collider.Aabb.Intersects(other.Aabb, -float.Epsilon))
        {
            var myAabb = collider.Aabb;
            var otherAabb = other.Aabb;

            var diff = myAabb.Bottom - otherAabb.Top;
            
            if ( diff > StepUpTolerance) return;
            
            Position -= new Vector2(0, diff);
        }
    }

    private void FixInside(ICollider collider, ICollider other)
    {
        var tries = 10;
        while (tries-- > 0 && collider.Aabb.Intersects(other.Aabb, -float.Epsilon))
        {
            var myAabb = collider.Aabb;
            var otherAabb = other.Aabb;

            Vector2 offset = Vector2.Zero;
            
            if (myAabb.Center.X < otherAabb.Center.X)
            {
                offset.X = otherAabb.Left - myAabb.Right;
            }
            
            if (myAabb.Center.X > otherAabb.Center.X)
            {
                offset.X = otherAabb.Right - myAabb.Left;
            }
            
            if (myAabb.Center.Y < otherAabb.Center.Y)
            {
                offset.Y = otherAabb.Top - myAabb.Bottom;
            }
            
            if (myAabb.Center.Y > otherAabb.Center.Y)
            {
                offset.Y = otherAabb.Bottom - myAabb.Top;
            }

            if (MathF.Abs(offset.X) < MathF.Abs(offset.Y))
            {
                Position += offset with { Y = 0 };
            }
            else
            {
                Position += offset with { X = 0 };
            }
        }
    }

    private void FixPosition(ICollider collider, ICollider other)
    {
        if (Velocity.Y < 0) return;

        var otherAabb = other.Aabb;
        var aabb = collider.Aabb;

        if (!collider.Material.Sides.HasFlag(ColliderSides.Bottom)) return;

        if (other.Type.HasFlag(ColliderType.Trigger)) return;

        if (otherAabb.Top - aabb.Bottom >= 0) return;
        if (!other.Material.Sides.HasFlag(ColliderSides.Top | ColliderSides.Left | ColliderSides.Right)) return;

        if (aabb.Bottom - otherAabb.Top > StepUpTolerance) return;

        Position = new Vector2(Position.X, Position.Y + otherAabb.Top - aabb.Bottom);
    }

    private void UpdateCollidersInTree()
    {
        if (Colliders == null) return;
        foreach (var collider in Colliders)
        {
            _simulation.UpdateColliderInTree(collider);
        }
    }

    public void RemoveCollider(ICollider collider)
    {
        if(Colliders == null) return;
        
        var colliders = Colliders.Where( o => !ReferenceEquals(collider, o)).ToArray();
        Colliders = colliders;
        
        _simulation.UpdateColliderInTree(collider, true);
    }

    public void Update(float timeInSeconds) => Owner?.OnUpdate(timeInSeconds);

    private static bool IsTriggerOrParticle(ICollider collider) => (collider.Type & (ColliderType.Trigger | ColliderType.Particle)) != 0;

    public ICollider FindCollider(string name)
    {
        for(var idx =0; idx < Colliders.Length; ++idx)
        {
            if(name == Colliders[idx].Name)
            {
                return Colliders[idx];
            }
        }
        return null;
    }

    public void DetachFromSimulation()
    {
        _simulation.DetachBody(this, true);
    }

    public void ReattachToSimulation()
    {
        _simulation.AttachBody(this);
        UpdateCollidersInTree();
    }

    public void AddEventsReceiver(IBodyEventsReceiver receiver)
    {
        if (!_eventsReceivers.Contains(receiver))
        {
            _eventsReceivers.Add(receiver);
        }
    }
    
    public void RemoveEventsReceiver(IBodyEventsReceiver receiver) => _eventsReceivers.Remove(receiver);

    public void Dispose()
    {
        _simulation.DetachBody(this, false);
        Owner?.Dispose();

        foreach (var rec in _eventsReceivers)
        {
            rec.OnBodyDisposed();
        }
    }

    public void PostOnColision(ICollider source, ICollider other, Vector2 impact)
    {
        foreach (var rec in _eventsReceivers)
        {
            rec.OnCollision(source, other, impact);
        }
    }

    public void PostUpdate()
    {
        Owner?.OnPostUpdate();
    }
}