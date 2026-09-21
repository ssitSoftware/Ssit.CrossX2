using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Stering;

public class SteeringBehavior<TObject>
{
    internal void Enter(TObject obj) => OnEnter(obj);
    internal void Exit(TObject obj) => OnExit(obj);
    internal bool Event(TObject obj, ISpriteEvent @event) => OnEvent(obj, @event);
    internal bool SequenceFinished(TObject obj, string name) => OnSequenceFinished(obj, name);
    internal bool Collision(TObject obj, ICollider source, ICollider other, Vector2 impact) => OnCollision(obj, source, other, impact);   
    internal bool Update(TObject obj, float dt) => OnUpdate(obj, dt);
    internal bool FixedUpdate(TObject obj, float dt) => OnFixedUpdate(obj, dt);
    
    protected virtual bool OnFixedUpdate(TObject obj, float dt) => false;
    protected virtual bool OnUpdate(TObject obj, float dt) => false;
    
    protected virtual bool OnEvent(TObject obj, ISpriteEvent @event) => false;
    
    protected virtual bool OnSequenceFinished(TObject obj, string name) => false;
    
    protected virtual void OnEnter(TObject character)
    {
    }

    protected virtual void OnExit(TObject obj)
    {
    }

    protected virtual bool OnCollision(TObject obj, ICollider source, ICollider other, Vector2 impact) => false;
}