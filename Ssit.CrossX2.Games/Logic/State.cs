namespace Ssit.CrossX2.Framework.Games.Logic;

public class State(params Behavior[] behaviors)
{
    public void Update(float dt) => OnUpdate( dt);

    protected virtual void OnUpdate(float dt)
    {
        for (var idx = 0; idx < behaviors.Length; ++idx)
        {
            if (behaviors[idx].Update(dt))
            {
                return;
            }
        }
    }

    public void FixedUpdate(float dt) => OnFixedUpdate(dt);

    protected virtual void OnFixedUpdate(float dt)
    {
        for (var idx = 0; idx < behaviors.Length; ++idx)
        {
            if (behaviors[idx].FixedUpdate(dt))
            {
                return;
            }
        }
    }

    public void PostFixedUpdate() => OnPostFixedUpdate();

    protected virtual void OnPostFixedUpdate()
    {
        for (var idx = 0; idx < behaviors.Length; ++idx)
        {
            if (behaviors[idx].PostFixedUpdate())
            {
                return;
            }
        }
    }

    public void Enter()
    {
        for (var idx = 0; idx < behaviors.Length; ++idx)
        {
            behaviors[idx].EnterState();
        }
    }
    
    public void Leave()
    {
        for (var idx = 0; idx < behaviors.Length; ++idx)
        {
            behaviors[idx].LeaveState();
        }
    }

    public void SequenceFinished(string name)
    {
        for (var idx = 0; idx < behaviors.Length; ++idx)
        {
            if (behaviors[idx].SequenceFinished(name))
            {
                return;
            }
        }
    }

    public void Event(string name, float parameter)
    {
        for (var idx = 0; idx < behaviors.Length; ++idx)
        {
            if (behaviors[idx].Event(name, parameter))
            {
                return;
            }
        }
    }
}