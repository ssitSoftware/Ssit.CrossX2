using System.Windows.Input;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Views;

public class IconCheckBox : Background, IButtonView
{
    public string UniqueId { get; set; }
    
    public ICommand Command { get; set; }
    public object CommandParameter { get; set; }
    public TimeSpan KeyCommandDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan CommandDelay { get; set; } = TimeSpan.FromMilliseconds(33);
    public string CommandSoundId { get; set; }
    public ButtonCommandType EnabledCommandTypes { get; set; } = ButtonCommandType.Select;
    public IUiSounds CustomSounds { get; set; }

    public ImageSource<ITexture> Image { get; set; }
    public ImageSource<ITexture> ImagePushed { get; set; }
    public Size IconSize { get; set; }
    
    public int FrameOn { get; set; } = 1;
    public int FrameOff { get; set; } = 0;

    public SharedBool IsChecked { get; set; } = new SharedBoolValue(false);
    public SharedValue<bool> HapticFeedback { get; set; } = false;
}