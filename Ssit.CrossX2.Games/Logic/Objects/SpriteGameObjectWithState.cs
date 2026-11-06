using Ssit.CrossX2.Framework.Games.Platformer.Builders;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class SpriteGameObjectWithState : SpriteGameObject
{
    private readonly StateMachine _stateMachine;

    protected SpriteGameObjectWithState(GameObjectsServices services, ObjectCreationParameters parameters)
        : base(services, parameters)
    {
        _stateMachine = new StateMachine(OnStateChanged);
    }

    public string CurrentState => _stateMachine.CurrentState;

    protected virtual bool StateMatchesSequence => true;

    protected void AddState(string name, State state) => _stateMachine.AddState(name, state);

    public void SetState(string state) => _stateMachine.SetState(state);

    protected virtual void InitializeSprite(string spritePath) => InitializeSprite(spritePath, true);

    protected override void OnFixedUpdate(ref bool cancelUpdate)
    {
        base.OnFixedUpdate(ref cancelUpdate);

        var dt = Services.Simulation.SimulationParameters.TimeDelta;
        _stateMachine.FixedUpdate(dt);
        OnStateChanged(CurrentState);
    }

    protected virtual void OnStateChanged(string state)
    {
        if (StateMatchesSequence)
        {
            Sprite.SetSequence(state);
        }
    }
}
