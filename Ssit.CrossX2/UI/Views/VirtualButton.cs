using Ssit.CrossX2.Input;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Views;

public class VirtualButton: Container
{
    public GameControllerButton Button { get; set; }
    public ColorWrapper ColorPressed { get; set; }
    public ColorWrapper OutlineColorPressed { get; set; }
    public SharedValue<bool> HapticFeedback { get; set; } = false;
}