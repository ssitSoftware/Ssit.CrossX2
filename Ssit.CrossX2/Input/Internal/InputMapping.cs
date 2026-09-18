namespace Ssit.CrossX2.Input.Internal;

internal class InputMapping : IMapper, IMapperInt, IInputMapping
{
    private readonly Dictionary<string, GameControllerAxis> _axisToAxisMappings = new ();
    private readonly Dictionary<string, (GameControllerButton, GameControllerButton)> _buttonToButtonMapping = new ();
    private readonly Dictionary<string, (Key, Key)> _keyToButtonMapping = new ();
    
    private readonly Dictionary<string, (GameControllerButton, GameControllerButton)> _buttonsToAxisMapping = new ();
    private readonly Dictionary<string, (Key, Key)> _keysToAxisMapping = new ();
    private readonly IGameControllers _gameControllers;
    private readonly IKeyboard _keyboard;
    private readonly IVirtualGameInput _virtualGameInput;
    private readonly int _player;

    public InputMapping(IGameControllers gameControllers, IKeyboard keyboard, IVirtualGameInput virtualGameInput, int player)
    {
        _gameControllers = gameControllers;
        _keyboard = keyboard;
        _virtualGameInput = virtualGameInput;
        _player = player;
    }

    public IMapper MapAxis(string axisName, GameControllerAxis axis)
    {
        _axisToAxisMappings[axisName] = axis;
        return this;
    }

    public IMapper MapAxis(string axisName, GameControllerButton negative, GameControllerButton positive)
    {
        _buttonsToAxisMapping[axisName] = (negative, positive);
        return this;
    }

    public IMapper MapAxis(string axisName, Key negative, Key positive)
    {
        _keysToAxisMapping[axisName] = (negative, positive);
        return this;
    }

    public IMapper MapButton(string buttonName, GameControllerButton button, GameControllerButton alt = GameControllerButton.None)
    {
        _buttonToButtonMapping[buttonName] = (button, alt);
        return this;
    }

    public IMapper MapButton(string buttonName, Key key, Key alt = Key.None)
    {
        _keyToButtonMapping[buttonName] = (key, alt);
        return this;
    }

    public IMapper Clear()
    {
        _axisToAxisMappings.Clear();
        _buttonToButtonMapping.Clear();
        _keyToButtonMapping.Clear();
        _buttonsToAxisMapping.Clear();
        _keysToAxisMapping.Clear();
        return this;
    }

    public ButtonState GetButton(string button)
    {
        bool isDown = false;
        bool wasDown = false;
        
        if (_buttonToButtonMapping.TryGetValue(button, out var btn))
        {
            var state = _gameControllers.GetButton(_player, btn.Item1);
            var state2 = _gameControllers.GetButton(_player, btn.Item2);
            
            isDown = state.IsDown || state2.IsDown;
            wasDown = state.WasDown || state2.WasDown;

            if (_player == 0)
            {
                var state3 = _virtualGameInput.GetButton(btn.Item1);
                var state4 = _virtualGameInput.GetButton(btn.Item2);
                
                isDown |= state3.IsDown || state4.IsDown;
                wasDown |= state3.WasDown || state4.WasDown;
            }
        }

        if (_keyToButtonMapping.TryGetValue(button, out var key))
        {
            var state = _keyboard.GetKey(key.Item1);
            var state2 = _keyboard.GetKey(key.Item2);

            isDown |= state.IsDown || state2.IsDown;
            wasDown |= state.WasDown || state2.WasDown;
        }

        return new(isDown, isDown != wasDown);
    }

    public void Feedback(Vibration low, Vibration high, uint ms)
    {
        _gameControllers.Vibrate(_player, low, high, ms);
    }

    public float GetAxis(string axis)
    {
        float value = 0;

        if (_axisToAxisMappings.TryGetValue(axis, out var axisId))
        {
            value = _gameControllers.GetAxis(_player, axisId);

            if (_player == 0)
            {
                value  += _virtualGameInput.GetAxis(axisId);
            }
        }

        if (_buttonsToAxisMapping.TryGetValue(axis, out var buttons))
        {
            value += _gameControllers.GetButton(_player, buttons.Item1).IsDown ? -1 : 0;
            value += _gameControllers.GetButton(_player, buttons.Item2).IsDown ? 1 : 0;

            if (_player == 0)
            {
                value += _virtualGameInput.GetButton(buttons.Item1).IsDown ? -1 : 0;
                value += _virtualGameInput.GetButton(buttons.Item2).IsDown ? 1 : 0;
            }
        }
        
        if (_keysToAxisMapping.TryGetValue(axis, out var keys))
        {
            value += _keyboard.GetKey(keys.Item1).IsDown ? -1 : 0;
            value += _keyboard.GetKey(keys.Item2).IsDown ? 1 : 0;
        }

        return MathF.Max(-1, MathF.Min(1, value));
    }

    public string[] GetMappedButtons()
    {
        var hashSet = new HashSet<string>(_buttonToButtonMapping.Keys);
        hashSet.UnionWith(_keyToButtonMapping.Keys);
        return hashSet.ToArray();
    }

    public string[] GetMappedAxes()
    {
        var hashSet = new HashSet<string>(_axisToAxisMappings.Keys);
        hashSet.UnionWith(_keysToAxisMapping.Keys);
        hashSet.UnionWith(_buttonsToAxisMapping.Keys);
        return hashSet.ToArray();
    }
}