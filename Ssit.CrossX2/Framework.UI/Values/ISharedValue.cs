namespace Ssit.CrossX2.Framework.UI.Values;

public interface ISharedValue<T>
{
    event Action<T> ValueChanged;
    T Value { get; set; }
}