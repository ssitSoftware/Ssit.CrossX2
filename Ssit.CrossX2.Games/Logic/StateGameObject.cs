using Ssit.CrossX2.Framework.Core;

namespace Ssit.CrossX2.Framework.Games.Logic;

public abstract class StateGameObject: IUpdatable
{
    private readonly StateMachine _stateMachine;

    protected StateGameObject()
    {
        _stateMachine = new StateMachine(OnStateChanged);
    }

    public string CurrentState => _stateMachine.CurrentState;

    protected void OnFixedUpdate(float dt)
    {
        _stateMachine.FixedUpdate(dt);
        OnStateChanged(CurrentState);
    }

    protected void AddState(string name, State state) => _stateMachine.AddState(name, state);

    public void SetState(string state) => _stateMachine.SetState(state);

    protected virtual void OnStateChanged(string state)
    {
    }
}
