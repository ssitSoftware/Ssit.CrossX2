using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public static class SpriteGameObjectStateMachineExtensions
{
    public static SpriteGameObjectStateMachine<TObject, TStateObject> WithBehaviorState<TObject, TStateObject>(
        this SpriteGameObjectStateMachine<TObject, TStateObject> sm, string name, string sequence, SteeringBehavior<TStateObject>[] behaviors) where TObject: SpriteGameObject2, TStateObject
    {
        var state = new SteeringStateWithBehaviors<TStateObject>(name, behaviors);
        
        return sm
            .RegisterState(state)
            .MapSequence(name, sequence);
    }

    public static SpriteGameObjectStateMachine<TObject, TStateObject> WithBehaviorState<TObject, TStateObject>(
        this SpriteGameObjectStateMachine<TObject, TStateObject> sm, string name, SteeringBehavior<TStateObject>[] behaviors)
        where TObject : SpriteGameObject2, TStateObject =>
        sm.WithBehaviorState(name, name, behaviors);
}