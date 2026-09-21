using System.Windows.Input;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class Button: Container, IButtonView
{
    public IButtonStateColors ForegroundColors { get; set; }
    public IButtonStateColors OutlineColors { get; set; }
    public IButtonStateColors BackgroundColors { get; set; }
    
    public TimeSpan KeyCommandDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan CommandDelay { get; set; } = TimeSpan.FromMilliseconds(33);
    
    public ICommand Command { get; set; }
    public object CommandParameter { get; set; }
    
    public string UniqueId { get; set; }
    public string CommandSoundId { get; set; }
    public ButtonCommandType EnabledCommandTypes { get; set; } = ButtonCommandType.Select;
    public IUiSounds CustomSounds { get; set; }
    public SharedValue<bool> HapticFeedback { get; set; } = false;
}