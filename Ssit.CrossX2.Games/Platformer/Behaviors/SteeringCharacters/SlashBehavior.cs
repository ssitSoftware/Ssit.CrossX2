using System.Numerics;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class SlashBehavior : SteeringBehavior<ISteeringCharacter>
{
    private readonly Dictionary<string, float> _powers = new();

    private readonly List<ICollider> _colliders = new();
    private readonly BoxHitDetector _boxHitDetector;

    /// <summary>Collider groups the slash can hit (see <see cref="IMaterial.Bit"/>). Default: everything.</summary>
    public ulong CollidesWith { get; init; } = IMaterial.AllColliders;
    
    public SlashBehavior(Vector2 offset, SizeF size, params (string, float)[] powers): this(new BoxHitDetector(offset, size), powers)
    {
    }
    
    public SlashBehavior(BoxHitDetector detector, params (string, float)[] powers)
    {
        _boxHitDetector = detector;
        
        foreach (var pow in powers)
        {
            _powers.Add(pow.Item1, pow.Item2);
        }
    }

    protected override bool OnEvent(ISteeringCharacter obj, ISpriteEvent @event)
    {
        if (!@event.EventName.Contains("Slash")) return false;
        if (!_powers.TryGetValue(@event.EventName, out var power)) return false;

        var result = _boxHitDetector.Detect(obj.Body, obj.FaceLeft, CollidesWith, Filter, ColliderSides.Left | ColliderSides.Right);

        result.Hittable?.Hit(obj.FaceLeft ? new Vector2(-1, 0) : new Vector2(1, 0), power, obj as IHittable);

        if (result.HitAnything)
        {
            obj.Body.Velocity += new Vector2(obj.FaceLeft ? obj.PhysicsValues.AttackVelocity : -obj.PhysicsValues.AttackVelocity, 0);
        }

        return false;
    }

    protected virtual bool Filter(IBody body, IHittable hittable) => true;
}