using System.Diagnostics.CodeAnalysis;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public sealed class VirtualSwitch(ObjectCreationParameters<VirtualSwitch.Parameters> parameters) : ISwitch
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Parameters
    {
        [Editor] public bool IsOn { get; set; }
    }
    
    public event Action OnChanged;
    public bool IsOn { get; private set; } = parameters.Parameters.IsOn;

    public void Toggle()
    {
        IsOn = !IsOn;
        OnChanged?.Invoke();
    }
}