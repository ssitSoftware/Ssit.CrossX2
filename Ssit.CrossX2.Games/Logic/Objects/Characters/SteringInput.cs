using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public class SteeringInput : ISteeringInputController
{
    private readonly Dictionary<string, ButtonState> _buttonStates = new();
    private readonly Dictionary<string, float> _values = new();
    
    private readonly Dictionary<string, string> _mappings = new();
    private readonly List<string> _buttonIds = new();
    private readonly List<string> _valueIds = new();
    private readonly IInputMapping _mapping;

    public SteeringInput(IInputMapping mapping = null)
    {
        _mapping = mapping;
    }

    public ButtonState Button(string id) => _buttonStates.GetValueOrDefault(id, ButtonState.Empty);
    public float Value(string id) => _values.GetValueOrDefault(id, 0.0f);
    
    public void MapButton(string id, string inputId)
    {
        _mappings[id] = inputId;
        _buttonIds.Add(id);
    }
    
    public void MapValue(string id, string axisId)
    {
        _mappings[id] = axisId;
        _valueIds.Add(id);
    }

    public void FixedUpdate()
    {
        if (_mapping is null)
            return;
        
        foreach (var id in _buttonIds)
        {
            if (_mappings.TryGetValue(id, out var idState))
            {
                var state = _mapping.GetButton(idState);
                var prevState = Button(id);
                    
                _buttonStates[id] = new ButtonState(state.IsDown, prevState.IsDown != state.IsDown);
            }
        }

        foreach (var id in _valueIds)
        {
            if (_mappings.TryGetValue(id, out var valueId))
            {
                _values[id] = _mapping.GetAxis(id);
            }
        }
    }

    public void SetValue(string id, float value) => _values[id] = value;
    public void SetButtonState(string id, ButtonState buttonState) => _buttonStates[id] = buttonState;
    public void FinishInitialization() => FixedUpdate();
}