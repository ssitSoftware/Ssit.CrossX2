using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Views.Markdown;

public class MarkdownMapper: IMarkdownMapper
{
    private readonly Dictionary<MarkdownStyle, FontDesc> _mappings = new();
    private readonly Dictionary<string, string> _imagePaths = new();
    
    private readonly Dictionary<string, string> _imageResolvers = new();
    
    private FontDesc _defaultFont = ("Default", 12);

    public MarkdownMapper With(MarkdownStyle style, FontDesc font)
    {
        _mappings.Add(style, font);
        return this;
    }

    public MarkdownMapper WithDefaultFont(FontDesc font)
    {
        _defaultFont = font;
        return this;
    }
    
    public MarkdownMapper WithImagePath(string path, string newPath)
    {
        _imagePaths.Add(path, newPath);
        return this;
    }

    public MarkdownMapper WithImageResolver(string prefix, string replace)
    {
        _imageResolvers.Add(prefix, replace);
        return this;
    }
    
    public FontDesc GetFont(MarkdownStyle style) => _mappings.GetValueOrDefault(style, _defaultFont);

    public string GetImagePath(string path)
    {
        if(_imagePaths.TryGetValue(path, out var newPath)) return newPath;
        
        var prefix = path.Split(':')[0];

        if (_imageResolvers.TryGetValue(prefix, out var postfix))
        {
            var imgPath = path.Split(':')[1];
            if (!imgPath.Contains('.'))
            {
                imgPath += ".png";
            }
            return postfix + imgPath;
        }

        return path;
    }
}