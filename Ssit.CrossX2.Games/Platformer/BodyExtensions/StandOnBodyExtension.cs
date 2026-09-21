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

    void IBodyEventsReceiver.OnBodyUpdated() => UpdateHookedColliders();
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
            if(!_hookedColliders.Contains(other)) _hookedColliders.Add(other);
        }
    }

    private void OnMove(Vector2 hookedMove)
    {
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