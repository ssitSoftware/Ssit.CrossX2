namespace Ssit.CrossX2.Framework.UI.Handlers.Markdown;

// This class was created with Claude Code assistance
internal class LayoutLine
{
    public float Y;
    public float Height;
    public float Width;
    public bool IsLastInBlock;
    public List<LayoutPiece> Pieces = new();
}