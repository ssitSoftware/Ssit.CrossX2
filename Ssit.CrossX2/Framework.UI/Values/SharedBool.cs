namespace Ssit.CrossX2.Framework.UI.Values;

public abstract class SharedBool
{
    public event Action ValueChanged;
    public abstract bool Value { get; }
    
    protected void RaiseValueChanged() => ValueChanged?.Invoke();

    public static implicit operator SharedBool(bool value) => new SharedBoolValue(value);
    public static SharedBool operator & (SharedBool value1, SharedBool value2) => new SharedBoolAnd(value1, value2);
    public static SharedBool operator | (SharedBool value1, SharedBool value2) => new SharedBoolOr(value1, value2);
    public static SharedBool operator ! (SharedBool value) => new SharedBoolNeg(value);
}

public class SharedBoolValue(bool value) : SharedBool
{
    public override bool Value { get; } = value;
}

public class SharedBoolMutable(bool value) : SharedBool
{
    private bool _value = value;

    public override bool Value => _value;

    public void SetValue(bool value)
    {
        if (_value != value)
        {
            _value = value;
            RaiseValueChanged();
        }
    }
}

public class SharedBoolOr: SharedBool
{
    private readonly SharedBool _value1;
    private readonly SharedBool _value2;
    public override bool Value => _value1.Value || _value2.Value;

    internal SharedBoolOr(SharedBool value1, SharedBool value2)
    {
        _value1 = value1;
        _value2 = value2;

        _value1.ValueChanged += RaiseValueChanged;
        _value2.ValueChanged += RaiseValueChanged;
    }
}

public class SharedBoolAnd: SharedBool
{
    private readonly SharedBool _value1;
    private readonly SharedBool _value2;
    public override bool Value => _value1.Value && _value2.Value;

    internal SharedBoolAnd(SharedBool value1, SharedBool value2)
    {
        _value1 = value1;
        _value2 = value2;

        _value1.ValueChanged += RaiseValueChanged;
        _value2.ValueChanged += RaiseValueChanged;
    }
}

public class SharedBoolNeg: SharedBool
{
    private readonly SharedBool _value;
    public override bool Value => !_value.Value;

    internal SharedBoolNeg(SharedBool value)
    {
        _value = value;
        _value.ValueChanged += RaiseValueChanged;
    }
}