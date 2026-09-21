using Ssit.CrossX2.Framework.Core;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class Updatable : IUpdatable
{
    private readonly List<IUpdatable> _updatables = [];

    internal void AddUpdatableInternal( IUpdatable updatable) => _updatables.Add(updatable);
    
    protected void AddUpdatable(IUpdatable updatable ) => _updatables.Add(updatable);
    
    void IUpdatable.Update(float dt)
    {
        OnUpdate(dt);
        
        foreach (var updatable in _updatables)
        {
            updatable.Update(dt);
        }
    }
    
    void IUpdatable.FixedUpdate(float dt)
    {
        OnFixedUpdate(dt);
        
        foreach (var updatable in _updatables)
        {
            updatable.FixedUpdate(dt);
        }
    }

    void IUpdatable.PostFixedUpdate()
    {
        OnPostFixedUpdate();
        
        foreach (var updatable in _updatables)
        {
            updatable.PostFixedUpdate();
        }
    }

    void IUpdatable.PostUpdate()
    {
        OnPostUpdate();
        
        foreach (var updatable in _updatables)
        {
            updatable.PostUpdate();
        }
    }

    protected virtual void OnFixedUpdate(float dt)
    {
    }

    protected virtual void OnUpdate(float dt)
    {
    }
    
    protected virtual void OnPostFixedUpdate()
    {
    }
    
    protected virtual void OnPostUpdate()
    {
    }
}