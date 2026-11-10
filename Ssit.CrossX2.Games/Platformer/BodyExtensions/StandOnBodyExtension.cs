// MIT License - Copyright © ebatianoSoftware
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.BodyExtensions;

public class StandOnBodyExtension: IBodyExtension, IBodyEventsReceiver
{
    private readonly List<ICollider> _hookedColliders = new();

    private readonly IBody _body;

    // Platform velocity accumulated during the current fixed update and the one from the previous update.
    // Bodies standing on the platform are updated before it (UpdateOrder = int.MaxValue), so when a body lands
    // during its update, _lastVelocity is the current velocity of the platform.
    private Vector2 _currentVelocity;
    private Vector2 _lastVelocity;

    void IBodyEventsReceiver.OnBodyUpdated()
    {
        _lastVelocity = _currentVelocity;
        _currentVelocity = Vector2.Zero;
        UpdateHookedColliders();
    }
    void IBodyEventsReceiver.OnBodyDisposed() => BodyExtensions.List.Remove(this);
    void IBodyEventsReceiver.OnBodyMoved(Vector2 move) => OnMove(move);

    void IBodyEventsReceiver.OnCollision(ICollider source, ICollider other, Vector2 impact) =>
        AabbCollider_CollisionWith(source, other, impact);
    
    public static void Attach(IBody body)
    {
        BodyExtensions.List.Add(new StandOnBodyExtension(body));
    }
    
    private StandOnBodyExtension(IBody body)
    {
        _body = body;
        _body.AddEventsReceiver(this);
        _body.UpdateOrder = int.MaxValue;
    }

    private void AabbCollider_CollisionWith(ICollider _, ICollider other, Vector2 impact)
    {
        if (other.Type != ColliderType.Dynamic) return;
        
        if (other.AttachedBody == null) return;
        if (other.Aabb.Top >= _body.Colliders[0].Aabb.Top) return;

        if (other.Aabb.Bottom - _body.Simulation.MovementEpsilon < _body.Colliders[0].Aabb.Top)
        {
            if (!_hookedColliders.Contains(other))
            {
                _hookedColliders.Add(other);
                OnLanded(other.AttachedBody);
            }
        }
    }

    // A body in the air carries velocity in world space (jumping from a moving platform adds the platform velocity to it).
    // Once it stands on the platform, it is moved along with it by OnMove, so its own velocity must become relative
    // to the platform. Otherwise the platform velocity is applied twice and the body slides after landing.
    private void OnLanded(IBody body)
    {
        if (body.IsKinematic) return;

        var platformVelocityX = _lastVelocity.X;
        var velocity = body.Velocity;

        // Only remove the platform's component the body actually carries: never reverse or amplify its movement,
        // so landing on a moving platform from static ground doesn't make the body slide backwards.
        if (MathF.Abs(platformVelocityX) < float.Epsilon || MathF.Sign(velocity.X) != MathF.Sign(platformVelocityX))
            return;

        var relativeX = MathF.Sign(velocity.X) * MathF.Max(0, MathF.Abs(velocity.X) - MathF.Abs(platformVelocityX));
        body.Velocity = velocity with { X = relativeX };
    }

    private void OnMove(Vector2 hookedMove)
    {
        _currentVelocity += hookedMove / _body.Simulation.SimulationParameters.TimeDelta;
        
        if (hookedMove.Y < 0) hookedMove.Y = 0;

        for (var idx = 0; idx < _hookedColliders.Count; ++idx)
        {
            var body = _hookedColliders[idx].AttachedBody;
            
            body.Move(hookedMove);
            body.KinematicVelocity += hookedMove / body.Simulation.SimulationParameters.TimeDelta;
        }

        UpdateHookedColliders();
    }

    private void UpdateHookedColliders()
    {
        var inflatedAabb = _body.Colliders[0].Aabb;
        inflatedAabb.Inflate(0.0001f);

        for (var idx = 0; idx < _hookedColliders.Count;)
        {
            if (!_hookedColliders[idx].Aabb.Intersects(inflatedAabb))
            {
                _hookedColliders.RemoveAt(idx);
                continue;
            }

            idx++;
        }
    }
}