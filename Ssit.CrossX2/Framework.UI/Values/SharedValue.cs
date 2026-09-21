namespace Ssit.CrossX2.Framework.UI.Values;

public class SharedValue<T>: ISharedValue<T>
{
    private T _value;

    public SharedValue()
    {
        _value = default;
    }
    
    public SharedValue(T value)
    {
        _value = value;
    }

    public event Action<T> ValueChanged;

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(value, _value))
                return;
            
            _value = value;
            ValueChanged?.Invoke(value);
        }
    }

    public static implicit operator SharedValue<T>(T value) => new(value);
}