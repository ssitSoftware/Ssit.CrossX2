using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public class TextInput: View
{
    public FontDesc? Font { get; set; }
    
    public SharedStringValue Text { get; set; }
    public SharedString Placeholder { get; set; }
    
    public InputType? InputType { get; set; }
    
    public Thickness? Padding { get; set; }
    
    public IButtonStateColors BackgroundColors { get; set; }
    
    public IButtonStateColors PlaceholderColors { get; set; }
    public IButtonStateColors PlaceholderOutlineColors { get; set; }
    
    public IButtonStateColors TextColors { get; set; }
    public IButtonStateColors TextOutlineColors { get; set; }
    
    public IButtonStateColors FrameColors { get; set; }
    public ColorWrapper? ActiveFrameColor { get; set; }
    public ColorWrapper? CursorColor { get; set; }
    
    public Length? FrameThickness { get; set; }
    public Length? ActiveFrameThickness { get; set; }
    public string UniqueId { get; set; }
    public TextScaling Scaling { get; set; } = TextScaling.Default;
    public SharedBool Enabled { get; set; }
    public TextUpdateMode? UpdateMode { get; set; }
    
    public Length? AdditionalKeyboardMargin { get; set; }
    public ColorWrapper? SelectionColor { get; set; }
}