using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public class SteeringInput : ISteeringInputController, ISteeringInput, IUpdatable
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

    /// <summary>
    /// Ids of the buttons that are down at the moment (in the last update).
    /// </summary>
    public List<string> GetPressedButtons() =>
        _buttonStates.Where(pair => pair.Value.IsDown).Select(pair => pair.Key).ToList();

    /// <summary>
    /// Marks the buttons as already held (<see cref="ButtonState.Down"/>), e.g. when the character is moved to a new
    /// game with the buttons still held - in the next update they stay down without being reported as just pressed
    /// (or are reported as just released). With auto mapping the buttons are mapped first.
    /// </summary>
    public void RestorePressedButtons(IEnumerable<string> ids)
    {
        foreach (var id in ids)
        {
            if (_autoMapping && !_mappings.ContainsKey(id))
            {
                MapButton(id, id);
            }

            _buttonStates[id] = ButtonState.Down;
        }
    }

    public void SetValue(string id, float value) => _values[id] = value;
    public void SetButtonState(string id, ButtonState buttonState) => _buttonStates[id] = buttonState;
    
    public void ResetManualInput()
    {
        _buttonStates.Clear();
        _values.Clear();
    }

    public void ProcessStatesManually()
    {
        foreach (var id in _buttonStates.Keys)
        {
            var state = _buttonStates[id];
            if (state == ButtonState.JustPressed)
            {
                _buttonStates[id] = ButtonState.Down;
            }
            else if (state == ButtonState.JustReleased)
            {
                _buttonStates[id] = ButtonState.Empty;
            }
        }
    }
}