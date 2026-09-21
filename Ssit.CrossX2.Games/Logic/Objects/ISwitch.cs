namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public interface ISwitch
{
    event Action OnChanged;
    bool IsOn { get; }
    void Toggle();
}