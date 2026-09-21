using System.Windows.Input;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;


public interface IButtonView: IFocusableView
{
    ICommand Command { get; }
    object CommandParameter { get; }
    TimeSpan KeyCommandDelay { get; }
    TimeSpan CommandDelay { get; }
    string CommandSoundId { get; }
    ButtonCommandType EnabledCommandTypes { get; }
    IUiSounds CustomSounds { get; }
    SharedValue<bool> HapticFeedback { get; }
}