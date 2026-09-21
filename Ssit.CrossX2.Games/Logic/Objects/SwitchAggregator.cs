using System.Diagnostics.CodeAnalysis;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public sealed class SwitchAggregator : ISwitch
{
    public enum Operation
    {
        And,
        Or,
        Xor
    }
    
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Parameters
    {
        [EditorLink(typeof(ISwitch))] public int Switch1 { get; set; }
        [EditorLink(typeof(ISwitch))] public int Switch2 { get; set; }
        [EditorLink(typeof(ISwitch))] public int Switch3 { get; set; }
        [EditorLink(typeof(ISwitch))] public int Switch4 { get; set; }
        
        [Editor] public bool Or { get; set; }
        [Editor] public bool Xor { get; set; }
        [Editor] public bool NegateResult { get; set; }
    }

    public event Action OnChanged;
    public bool IsOn { get; private set; }

    private readonly ISwitch[] _switches = new ISwitch[4];

    private readonly Operation _operation;
    private readonly bool _negateResult;
    
    public SwitchAggregator(ObjectCreationParameters<Parameters> parameters)
    {
        var ids = new int[4];
        
        ids[0] = parameters.Parameters.Switch1;
        ids[1] = parameters.Parameters.Switch2;
        ids[2] = parameters.Parameters.Switch3;
        ids[3] = parameters.Parameters.Switch4;

        _operation = parameters.Parameters.Or ? Operation.Or : parameters.Parameters.Xor ? Operation.Xor : Operation.And;
        _negateResult = parameters.Parameters.NegateResult;
        
        for (var idx = 0; idx < 4; ++idx)
        {
            var index = idx;
            parameters.LinkMap.RequestLink<ISwitch>(ids[idx], obj =>
            {
                _switches[index] = obj;
                if (obj is not null)
                {
                    obj.OnChanged += UpdateValue;
                }
                UpdateValue();
            });
        }
        
        UpdateValue();
    }

    private void UpdateValue()
    {
        bool result = false;
        bool oldValue = IsOn;

        switch (_operation)
        {
            case Operation.And:
                result = And();
                break;
            
            case Operation.Or:
                result = Or();
                break;
            
            case Operation.Xor:
                result = Xor();
                break;
        }
        
        var newValue =_negateResult ? !result : result;
        
        if (newValue != oldValue)
        {
            IsOn = newValue;
            OnChanged?.Invoke();
        }
    }

    private bool Xor()
    {
        var result = false;
        
        for(var idx =0; idx < _switches.Length; idx++)
        {
            if (_switches[idx] != null)
            {
                result ^= _switches[idx].IsOn;
            }
        }
        
        return result;
    }

    private bool Or()
    {
        for(var idx =0; idx < _switches.Length; idx++)
        {
            if (_switches[idx] != null && _switches[idx].IsOn)
            {
                return true;
            }
        }

        return false;
    }

    private bool And()
    {
        for(var idx =0; idx < _switches.Length; idx++)
        {
            if (_switches[idx] != null && !_switches[idx].IsOn)
            {
                return false;
            }
        }

        return true;
    }

    public void Toggle()
    {
    }
}