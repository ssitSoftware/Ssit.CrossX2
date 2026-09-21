using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Stering;

public class SteeringStateWithBehaviors<TObject>(string name, params SteeringBehavior<TObject>[] behaviors) : SteeringState<TObject>
{
    public override string Name => name;

    protected override void OnEnter(TObject obj)
    {
        foreach (var behavior in behaviors)
        {
            behavior.Enter(obj);
        }
    }
    
    protected override void OnExit(TObject obj)
    {
        foreach (var behavior in behaviors)
        {
            behavior.Exit(obj);
        }
    }

    protected override void OnUpdate(TObject obj, float dt)
    {
        foreach (var behavior in behaviors)
        {
            if (behavior.Update(obj, dt))
            {
                break;
            }
        }
    }
    
    protected override void OnFixedUpdate(TObject obj, float dt)
    {
        foreach (var behavior in behaviors)
        {
            if (behavior.FixedUpdate(obj, dt))
            {
                break;
            }
        }
    }

    protected override void OnEvent(TObject obj, ISpriteEvent @event)
    {
        foreach (var behavior in behaviors)
        {
            if (behavior.Event(obj, @event))
            {
                break;
            }
        }
    }

    protected override void OnSequenceFinished(TObject obj, string sequenceName)
    {
        foreach (var behavior in behaviors)
        {
            if (behavior.SequenceFinished(obj, sequenceName))
            {
                break;
            }
        }
    }
    
    protected override void OnCollision(TObject obj, ICollider source, ICollider other, Vector2 impact)
    {
        foreach (var behavior in behaviors)
        {
            if (behavior.Collision(obj, source, other, impact))
            {
                break;
            }
        }
    }
}