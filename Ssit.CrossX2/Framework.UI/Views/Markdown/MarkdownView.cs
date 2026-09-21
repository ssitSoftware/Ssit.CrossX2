using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views.Markdown;

public class MarkdownView: Background
{
    public SharedString Text { get; set; }

    public ColorWrapper? TextColor { get; set; }
    public ColorWrapper? TextOutlineColor { get; set; }
    public TextScaling Scaling { get; set; }
    public ContentAlign? TextAlign { get; set; }

    public IMarkdownMapper Mapper { get; set; }

    public Thickness? Padding { get; set; }

    /// <summary>Extra spacing in pixels between lines within a block. Negative means no extra spacing.</summary>
    public float LineSpacing { get; set; } = -1f;

    /// <summary>Spacing in pixels between blocks. Negative means use the default per-block margin.</summary>
    public float ParagraphSpacing { get; set; } = -1f;
}