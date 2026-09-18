using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Views.Markdown;

public interface IMarkdownMapper
{
    FontDesc GetFont(MarkdownStyle style);
    string GetImagePath(string path);
}