using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class HitInRangeBehavior(SizeF size, Vector2 offset, float attackPower = 1) : SteeringBehavior<ISteeringCharacter>
{
    private readonly List<ICollider> _colliders = new();
    
    // ReSharper disable once ClassNeverInstantiated.Local
    private class Parameters
    {
        public bool CanHit;
    }

    protected override void OnEnter(ISteeringCharacter obj) => SetCanHit(obj, false);
    protected override void OnExit(ISteeringCharacter obj) => SetCanHit(obj, false);

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<Parameters>(true);
        if (!parameters.CanHit)
            return false;
        
        var flippedOffset = new Vector2(obj.FaceLeft ? -offset.X : offset.X, offset.Y - size.Height / 2);
        var center = obj.Body.Position + flippedOffset;

        var aabb = new Aabb(center, size);
        
        _colliders.Clear();
        obj.Body.Simulation.CheckCollision(aabb, obj.Body, colliders: _colliders, colliderType: ColliderType.Dynamic | ColliderType.Trigger, debugRegister: true);

        foreach (var collider in _colliders)
        {
            if (collider.AttachedBody?.Owner is not IHittable { Alive: true } hittable)
                continue;

            hittable.Hit(new Vector2(obj.FaceLeft ? -1 : 1, 0), attackPower);
        }
        
        return false;
    }

    protected override bool OnEvent(ISteeringCharacter obj, ISpriteEvent @event)
    {
        switch (@event.EventName)
        {
            case "Begin":
                SetCanHit(obj, true);
                break;
            
            case "End":
                SetCanHit(obj, false);
                break;
        }
        return false;
    }
    
    private static void SetCanHit(ISteeringCharacter obj, bool value)
    {
        var parameters = obj.GetParameters<Parameters>(true);
        parameters.CanHit = value;
    }
}
