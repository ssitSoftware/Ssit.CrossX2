using System.Numerics;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Melee attack hit detection driven by sprite events. Events registered in the powers table (any name, e.g. "Slash", "Bite")
/// trigger a box hit test in front of the character; the first hittable is hit with that event's power.
/// </summary>
public class SlashBehavior : SteeringBehavior<ISteeringCharacter>
{
    // Event name -> attack power
    private readonly Dictionary<string, float> _powers = new();

    private readonly List<ICollider> _colliders = new();
    private readonly BoxHitDetector _boxHitDetector;
    
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
        if (!_powers.TryGetValue(@event.EventName, out var power)) return false;

        // Test the hit box in front of the character (facing-aware), only against left/right collider sides
        var result = _boxHitDetector.Detect(obj.Body, obj.FaceLeft, IMaterial.AllColliders, Filter, ColliderSides.Left | ColliderSides.Right);

        result.Hittable?.Hit(obj.FaceLeft ? new Vector2(-1, 0) : new Vector2(1, 0), power, obj as IHittable);

        // Small recoil away from the target when the slash connected with anything (also walls)
        if (result.HitAnything)
        {
            obj.Body.Velocity += new Vector2(obj.FaceLeft ? obj.PhysicsValues.AttackVelocity : -obj.PhysicsValues.AttackVelocity, 0);
        }

        return false;
    }

    /// <summary>True if the event is one of this behavior's attack events (registered in the powers table).</summary>
    protected bool IsAttackEvent(string eventName) => _powers.ContainsKey(eventName);

    /// <summary>
    /// Override to exclude some targets from being hit (e.g. allies).
    /// </summary>
    protected virtual bool Filter(IBody body, IHittable hittable) => true;
}