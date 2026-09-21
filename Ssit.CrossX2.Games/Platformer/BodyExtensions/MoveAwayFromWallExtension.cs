using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.BodyExtensions;

public class MoveAwayFromWallExtension : IBodyExtension, IBodyEventsReceiver
{
    private readonly IBody _body;
    private readonly List<ICollider> _collidersBuffer = new();

    void IBodyEventsReceiver.OnBodyMoved(Vector2 offset) => OnBodyMoved(offset);
    void IBodyEventsReceiver.OnBodyDisposed() => BodyExtensions.List.Remove(this);
    
    public static void Attach(IBody body)
    {
        BodyExtensions.List.Add(new MoveAwayFromWallExtension(body));
    }
    
    private MoveAwayFromWallExtension(IBody body)
    {
        _body = body;
        _body.AddEventsReceiver(this);
    }

    private void OnBodyMoved(Vector2 vector)
    {
        var movementEpsilon = _body.Simulation.MovementEpsilon;
        foreach (var collider in _body.Colliders)
        {
            var aabb = collider.Aabb;
            _collidersBuffer.Clear();
            
            var group = collider.Material.ColliderGroup;
            
            if (!_body.Simulation.CheckCollision(new Aabb(aabb.Left, aabb.Top + 0.001f, aabb.Right, aabb.Bottom - 0.001f), _body, group, 0, _collidersBuffer)) return;

            for (var idx = 0; idx < _collidersBuffer.Count; ++idx)
            {
                var otherCollider = _collidersBuffer[idx];
                var colliderAabb = otherCollider.Aabb;

                if (!otherCollider.Material.Sides.HasFlag(ColliderSides.Top | ColliderSides.Bottom)) continue;

                if (aabb.Left < colliderAabb.Left && aabb.Right > colliderAabb.Left && otherCollider.Material.Sides.HasFlag(ColliderSides.Left))
                {
                    _body.Move(new Vector2(colliderAabb.Left-aabb.Right - movementEpsilon, 0));
                    _body.Velocity = _body.Velocity with { X = 0 };
                }
                else if (aabb.Right > colliderAabb.Right && aabb.Left < colliderAabb.Right && otherCollider.Material.Sides.HasFlag(ColliderSides.Right))
                {
                    _body.Move(new Vector2(colliderAabb.Right - aabb.Left + movementEpsilon, 0));
                    _body.Velocity = _body.Velocity with { X = 0 };
                }
            }
        }
    }
}