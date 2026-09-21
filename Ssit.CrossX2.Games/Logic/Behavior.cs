namespace Ssit.CrossX2.Framework.Games.Logic;

public abstract class Behavior
{
    internal bool Update(float dt) => OnUpdate(dt);
    internal bool FixedUpdate(float dt) => OnFixedUpdate(dt);
    internal bool PostFixedUpdate() => OnPostFixedUpdate();
    internal void EnterState() => OnEnterState();
    internal void LeaveState() => OnLeaveState();

    internal bool SequenceFinished(string name) => OnSequenceFinished(name);
    
    protected virtual bool OnSequenceFinished(string name) => false;
    protected virtual bool OnEvent(string name, float parameter) => false;
    
    protected virtual void OnLeaveState()
    {
    }

    protected virtual void OnEnterState()
    {
    }

    protected virtual bool OnUpdate(float dt) => false;
    protected virtual bool OnFixedUpdate(float dt) => false;
    protected virtual bool OnPostFixedUpdate() => false;
    
    internal bool Event(string name, float parameter) => OnEvent(name, parameter);
}