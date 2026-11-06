namespace Ssit.CrossX2.Framework.Games.Logic;

public abstract class Behavior
{
    internal bool FixedUpdate(float dt) => OnUpdate(dt);
    internal void EnterState() => OnEnterState();
    internal void LeaveState() => OnLeaveState();
    
    protected virtual void OnLeaveState()
    {
    }

    protected virtual void OnEnterState()
    {
    }

    protected virtual bool OnUpdate(float dt) => false;
}