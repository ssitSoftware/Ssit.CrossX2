using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class VirtualButton: Container
{
    public GameControllerButton Button { get; set; }
    public ColorWrapper ColorPressed { get; set; }
    public ColorWrapper OutlineColorPressed { get; set; }
    public SharedValue<bool> HapticFeedback { get; set; } = false;
}