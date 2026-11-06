namespace Ssit.CrossX2.Framework.Games.Logic;

public class State(params Behavior[] behaviors)
{
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
}