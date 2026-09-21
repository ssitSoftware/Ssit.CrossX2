using Ssit.CrossX2.Framework.Core;

namespace Ssit.CrossX2.Framework.Games.Logic;

public abstract class StateGameObject: IUpdatable
{
    private readonly Dictionary<string, State> _states = new();
    private State _currentState;

    protected virtual void OnAnimationFinished(string sequenceName) => _currentState?.SequenceFinished(sequenceName);
    
    protected virtual void CallStateEvent(string eventName, float parameter) => _currentState?.Event(eventName, parameter);
    
    public string CurrentState { get; private set; }
    

    protected virtual void OnUpdate(float dt)
    {
        _currentState?.Update(dt);
        SetSequence(CurrentState);
    }
    
    protected void OnFixedUpdate(float dt)
    {
        _currentState?.FixedUpdate(dt);
        SetSequence(CurrentState);
    }
    
    protected virtual void OnPostFixedUpdate()
    {
        _currentState?.PostFixedUpdate();
        SetSequence(CurrentState);
    }

    protected void AddState(string name, State state)
    {
        _states.Add(name, state);
    }

    public void SetState(string state)
    {
        if (CurrentState == state)
            return;
        
        if (!_states.TryGetValue(state, out var instance))
            throw new InvalidOperationException();

        _currentState?.Leave();
        _currentState = instance;
        
        CurrentState = state;
        SetSequence(state);
        
        _currentState?.Enter();
    }

    protected virtual void SetSequence(string state)
    {
    }
}