using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public class SteeringInput : ISteeringInputController, IUpdatable
{
    private readonly Dictionary<string, ButtonState> _buttonStates = new();
    private readonly Dictionary<string, float> _values = new();
    
    private readonly Dictionary<string, string> _mappings = new();
    private readonly List<string> _buttonIds = new();
    private readonly List<string> _valueIds = new();
    private readonly IInputMapping _mapping;
    private readonly bool _autoMapping;

    public SteeringInput(IInputMapping mapping = null, bool autoMapping = false)
    {
        _mapping = mapping;
        _autoMapping = autoMapping;
    }

    public ButtonState Button(string id)
    {
        if (_autoMapping && !_mappings.ContainsKey(id))
        {
            MapButton(id, id);
        }

        if (!_buttonStates.ContainsKey(id))
        {
            _buttonStates[id] = ButtonState.Empty;
        }

        return _buttonStates.GetValueOrDefault(id, ButtonState.Empty);
    }

    public float Value(string id)
    {
        if (_autoMapping && !_mappings.ContainsKey(id))
        {
            MapValue(id, id);
        }

        _values.TryAdd(id, 0.0f);

        return _values.GetValueOrDefault(id, 0.0f);
    }

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

    private void AnalyzeButton(string id)
    {
        if (_mappings.TryGetValue(id, out var idState))
        {
            var state = _mapping.GetButton(idState);
            var prevState = Button(id);
                    
            _buttonStates[id] = new ButtonState(state.IsDown, prevState.IsDown != state.IsDown);
        }
    }

    private void AnalyzeValue(string id)
    {
        if (_mappings.TryGetValue(id, out var valueId))
        {
            _values[id] = _mapping.GetAxis(id);
        }
    }
    
    void IUpdatable.FixedUpdate(float dt)
    {
        FixedUpdate();
    }
    
    public void FixedUpdate()
    {
        if (_mapping is null)
            return;

        foreach (var id in _buttonIds)
        {
            AnalyzeButton(id);
        }

        foreach (var id in _valueIds)
        {
            AnalyzeValue(id);
        }
    }

    public void SetValue(string id, float value) => _values[id] = value;
    public void SetButtonState(string id, ButtonState buttonState) => _buttonStates[id] = buttonState;
}