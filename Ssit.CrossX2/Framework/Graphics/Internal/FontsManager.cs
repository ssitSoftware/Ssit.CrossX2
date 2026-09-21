using Newtonsoft.Json;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Graphics.Misc;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Graphics.Internal;

internal class FontsManager: IFontsManager, IDisposable
{
    private class FontsJson
    {
        public string[] Fonts { get; set; }
    }
    
    private readonly IIoCContainer _container;
    private readonly IFilesProvider _filesProvider;

    private readonly Dictionary<string, List<IGlyphFont>> _fonts = new();
    
    public FontsManager(IIoCContainer container, IFilesProvider filesProvider)
    {
        _container = container;
        _filesProvider = filesProvider;
    }
    
    public void Dispose()
    {
        var lists = _fonts.Select(x => x.Value).ToArray();
        foreach (var list in lists)
        {
            list.ForEach(o=>o.Dispose());
        }
        
        _fonts.Clear();
    }

    public void SetDefaultFont(string name)
    {
        if (_fonts.TryGetValue(name, out var fonts))
        {
            _fonts["Default"] = fonts;
        }
        else throw new Exception($"Font {name} not found");
    }

    public void LoadFonts(string fontsJsonPath)
    {
        using var stream = _filesProvider.Open(fontsJsonPath);
        
        var paths = JsonConvert.DeserializeObject<FontsJson>(new StreamReader(stream).ReadToEnd());
        var dir = Path.GetDirectoryName(fontsJsonPath);

        foreach (var path in paths.Fonts)
        {
            var fontPath = Path.Combine(dir ?? "", path);
            var font = _container.IoCConstruct<GraphicGlyphFont>(fontPath);

            if (!_fonts.TryGetValue(font.Name, out var list))
            {
                list = new List<IGlyphFont>();
                _fonts.Add(font.Name, list);
            }
            list.Add(font);
        }
    }

    public void LoadBitmapFont(string name, string path, Size size)
    {
        using var stream = _filesProvider.Open(path);
        
        var loadParameters = new LoadTextureParameters
        {
            DiffuseMapStream = stream
        };
        
        var texture = _container.IoCConstruct<ITexture>(loadParameters);
        
        var bitmapFont = new BitmapFont(name, size, texture, ' ', '}');
        if (!_fonts.TryGetValue(bitmapFont.Name, out var list))
        {
            list = new List<IGlyphFont>();
            _fonts.Add(bitmapFont.Name, list);
        }
        list.Add(bitmapFont);
    }

    public IFont GetFont(string name, float size = 0)
    {
        var diff = float.MaxValue;
        IFont retFont = null;
        
        if (_fonts.TryGetValue(name, out var fonts))
        {
            foreach (var font in fonts)
            {
                if (Math.Abs(font.Size - size) < diff)
                {
                    diff = MathF.Abs(font.Size - size);
                    retFont = font;
                }
            }
        }

        return retFont;
    }
}