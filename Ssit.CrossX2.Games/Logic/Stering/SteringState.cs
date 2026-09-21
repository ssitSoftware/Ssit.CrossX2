using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Stering;

public abstract class SteeringState<TObject>
{
    public abstract string Name { get; }
    
    internal void Enter(TObject obj) => OnEnter(obj);
    internal void Exit(TObject obj) => OnExit(obj);
    
    public void Update(TObject obj, float dt) => OnUpdate(obj, dt);
    public void FixedUpdate(TObject obj, float dt) => OnFixedUpdate(obj, dt);
    
    public void SequenceFinished(TObject obj, string name) => OnSequenceFinished(obj, name);
    public void Event(TObject obj, ISpriteEvent @event) => OnEvent(obj, @event);
    
    public void Collission(TObject obj, ICollider source, ICollider other, Vector2 impact) => OnCollision(obj, source, other, impact);   
    
    protected virtual void OnCollision(TObject obj, ICollider source, ICollider other, Vector2 impact)
    {
    }
    
    protected virtual void OnUpdate(TObject obj, float dt)
    {
    }
    
    protected virtual void OnFixedUpdate(TObject obj, float dt)
    {
    }

    protected virtual void OnEnter(TObject obj)
    {
    }

    protected virtual void OnExit(TObject obj)
    {
    }

    protected virtual void OnSequenceFinished(TObject obj, string name)
    {
    }

    protected virtual void OnEvent(TObject obj, ISpriteEvent @event)
    {
    }
}