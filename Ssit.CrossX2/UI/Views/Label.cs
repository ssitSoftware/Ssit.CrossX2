using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Views;

public class Label: Background
{
    public SharedString Text { get; set; }
    public ColorWrapper? TextColor { get; set; }
    public ColorWrapper? TextOutlineColor { get; set; }
    
    public ContentAlign? TextAlign { get; set; }
    public TextSpacing? TextSpacing { get; set; }
    public int? LineSpacing { get; set; }
    public float ParagraphSpacing { get; set; } = -1;
    public FontDesc? Font { get; set; }
    
    public Thickness? Padding { get; set; }
    public TextScaling Scaling { get; set; }
}