using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public class GameStateSwitch : ISwitch
{
    public class Parameters
    {
        [Editor] public string RequiredFlag { get; set; }
        [Editor] public bool Inverse { get; set; }
    }
    
    private readonly ISimulation _simulation;
    private readonly IGameState _gameState;

    private readonly string _requiredFlag;
    private readonly bool _inverse;
    
    public event Action OnChanged;
    
    public bool IsOn { get; private set; }
    
    public GameStateSwitch(GameObjectsServices services, IGameState gameState, ObjectCreationParameters<Parameters> parameters)
    {
        _simulation = services.Simulation;
        _gameState = gameState;
        
        _simulation.Disposed += OnSimulationDisposed;
        _gameState.StateUpdated += OnStateUpdated;
        
        _requiredFlag = parameters.Parameters.RequiredFlag;
        _inverse = parameters.Parameters.Inverse;
        
        IsOn = _gameState.HasFlag(_requiredFlag) ^ _inverse;
    }

    private void OnStateUpdated()
    {
        IsOn = _gameState.HasFlag(_requiredFlag) ^ _inverse;
        OnChanged?.Invoke();
    }

    private void OnSimulationDisposed()
    {
        _simulation.Disposed -= OnSimulationDisposed;
        _gameState.StateUpdated -= OnStateUpdated;
    }
    
    public void Toggle()
    {
        throw new NotSupportedException();
    }
}