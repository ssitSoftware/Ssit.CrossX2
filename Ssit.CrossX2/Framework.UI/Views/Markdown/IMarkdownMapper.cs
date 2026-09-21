using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views.Markdown;

public interface IMarkdownMapper
{
    FontDesc GetFont(MarkdownStyle style);
    string GetImagePath(string path);
}