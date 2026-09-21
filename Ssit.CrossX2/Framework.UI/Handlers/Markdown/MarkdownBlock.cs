namespace Ssit.CrossX2.Framework.UI.Handlers.Markdown;

// This class was created with Claude Code assistance
internal class MarkdownBlock
{
    public MarkdownBlockType Type;
    public readonly List<InlineSpan> Spans = new();
    public string ImagePath;
    public float MarginBottom;
}