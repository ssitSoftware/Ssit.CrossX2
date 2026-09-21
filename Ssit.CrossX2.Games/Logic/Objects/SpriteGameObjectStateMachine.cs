using System.Diagnostics;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public class SpriteGameObjectStateMachine<TObject, TStateObject> : SpriteInstance.IHandler, IUpdatable where TObject: SpriteGameObject2, TStateObject
{
    private readonly TObject _obj;
    public SteeringStateMachine<TStateObject> InternalStateMachine { get; }
    
    private readonly Dictionary<string, string> _sequenceMapping = new();
    private readonly Dictionary<string, SteeringState<TStateObject>> _SteeringStates = new();
    
    public SpriteGameObjectStateMachine(TObject obj)
    {
        _obj = obj;
        obj.AddUpdatableInternal(this);
        
        InternalStateMachine = new SteeringStateMachine<TStateObject>(obj);
        InternalStateMachine.OnStateChanged += OnStateChanged;

        obj.Sprite.Handler = this;
    }

    public void SetSteeringState(string stateName)
    {
        if (stateName.Equals(InternalStateMachine.CurrentState?.Name ?? ""))
            return;
        
        if (!_SteeringStates.TryGetValue(stateName, out var state))
        {
            throw new Exception($"State {stateName} not found");
        }
        
        InternalStateMachine.SetState(state);
        Debug.WriteLine($"{GetType().Name} Steering State: {stateName}");
    }
    
    public SpriteGameObjectStateMachine<TObject, TStateObject> RegisterState(SteeringState<TStateObject> state)
    {
        var name =  state.Name;
        _SteeringStates.Add(name, state);
        return this;
    }

    public SpriteGameObjectStateMachine<TObject, TStateObject> MapSequence(string stateName, string sequenceName)
    {
        _sequenceMapping[stateName] = sequenceName;
        return this;
    }
    
    private void OnStateChanged(object sender, SteeringState<TStateObject> state)
    {
        if (!_sequenceMapping.TryGetValue(state.Name, out var sequenceName))
        {
            sequenceName = state.Name;
        }
        
        _obj.Sprite.SetSequence(sequenceName);
    }

    void SpriteInstance.IHandler.OnSpriteEvent(SpriteInstance instance, ISpriteEvent @event)
    {
        InternalStateMachine.CurrentState?.Event(_obj, @event);
        _obj.CallSpriteEvent(@event);
    }

    void SpriteInstance.IHandler.OnSequenceFinished(SpriteInstance instance, string sequenceName, bool reverse)
    {
        InternalStateMachine.CurrentState?.SequenceFinished(_obj, sequenceName);
        _obj.CallSequenceFinished(sequenceName);
    }
    
    void IUpdatable.Update(float dt)
    {
        InternalStateMachine.CurrentState?.Update(_obj, dt);
    }
    
    void IUpdatable.FixedUpdate(float dt)
    {
        InternalStateMachine.CurrentState?.FixedUpdate(_obj, dt);
    }
}