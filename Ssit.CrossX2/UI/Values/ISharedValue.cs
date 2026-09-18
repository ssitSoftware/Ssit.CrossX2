namespace Ssit.CrossX2.UI.Values;

public interface ISharedValue<T>
{
    event Action<T> ValueChanged;
    T Value { get; set; }
}