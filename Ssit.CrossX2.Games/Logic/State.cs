namespace Ssit.CrossX2.Framework.Games.Logic;

public class State<TObject>(params Behavior<TObject>[] behaviors)
{
    public void FixedUpdate(TObject obj, float dt) => OnFixedUpdate(obj, dt);

    protected virtual void OnFixedUpdate(TObject obj, float dt)
    {
        for (var idx = 0; idx < behaviors.Length; ++idx)
        {
            if (behaviors[idx].FixedUpdate(obj, dt))
            {
                return;
            }
        }
    }

    public void Enter(TObject obj)
    {
        for (var idx = 0; idx < behaviors.Length; ++idx)
        {
            behaviors[idx].EnterState(obj);
        }
    }

    public void Leave(TObject obj)
    {
        for (var idx = 0; idx < behaviors.Length; ++idx)
        {
            behaviors[idx].LeaveState(obj);
        }
    }
}
