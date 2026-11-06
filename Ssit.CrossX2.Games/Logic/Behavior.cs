namespace Ssit.CrossX2.Framework.Games.Logic;

public abstract class Behavior<TObject>
{
    internal bool FixedUpdate(TObject obj, float dt) => OnUpdate(obj, dt);
    internal void EnterState(TObject obj) => OnEnterState(obj);
    internal void LeaveState(TObject obj) => OnLeaveState(obj);

    protected virtual void OnLeaveState(TObject obj)
    {
    }

    protected virtual void OnEnterState(TObject obj)
    {
    }

    protected virtual bool OnUpdate(TObject obj, float dt) => false;
}
