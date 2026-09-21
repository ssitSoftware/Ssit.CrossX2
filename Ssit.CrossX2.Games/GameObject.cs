using System.Numerics;
using SkiaSharp;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Sprites;
using Ssit.CrossX2.Framework.Graphics.Sprites.Json;
using Ssit.CrossX2.Framework.IO;

namespace Ssit.CrossX2.Framework.Games;

public class GameObject: IDisposable
{
    private readonly IContentManager _contentManager;

    public readonly JsonSpriteObjectDescription Description;

    public readonly bool HasSprite;
    public readonly bool HasTexture;

    public readonly string ResourcePath;

    public static (JsonSpriteObjectDescription, string, bool, bool) Parse(string path, IFilesProvider filesProvider, ContentAlign originAlignment = ContentAlign.Center | ContentAlign.VCenter)
    {
        var hasSprite = filesProvider.FileExists(path + ".json");
        var hasTexture = false;
        
        var resourcePath = "";
        
        if (hasSprite)
        {
            resourcePath = path + ".json";
        }
        else
        {
            hasTexture = filesProvider.FileExists(path + ".png");
            if (hasTexture)
            {
                resourcePath = path + ".png";
            }
        }
        
        Size sourceSize = Size.Zero;
        
        if (hasSprite)
        {
            var sprite = (Sprite)JsonSpriteLoader.Load(resourcePath, filesProvider);
            sourceSize = sprite.SourceSize;
        }

        if (hasTexture)
        {
            using var stream = filesProvider.Open(resourcePath);
            using var bmp = SKBitmap.Decode(stream);
            sourceSize = new Size(bmp.Width, bmp.Height);
        }
        
        JsonSpriteObjectDescription desc;
        try
        {
            using var stream = filesProvider.Open(path + ".desc.json");
            var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            desc = new JsonSpriteObjectDescription(json, sourceSize);
        }
        catch
        {
            var origin = Vector2.Zero;

            if (originAlignment.HasFlag(ContentAlign.Center))
            {
                origin.X = sourceSize.Width / 2f;
            }
            
            if (originAlignment.HasFlag(ContentAlign.Right))
            {
                origin.X = sourceSize.Width;
            }
            
            if (originAlignment.HasFlag(ContentAlign.VCenter))
            {
                origin.Y = sourceSize.Height / 2f;
            }
            
            if (originAlignment.HasFlag(ContentAlign.Bottom))
            {
                origin.Y = sourceSize.Height;
            }
            
            desc = new JsonSpriteObjectDescription(origin, sourceSize);
        }
        
        return (desc, resourcePath, hasSprite, hasTexture);
    }
    
    public static GameObject FromPath(string path, IFilesProvider filesProvider, ContentAlign alignment = ContentAlign.Center | ContentAlign.VCenter) => new(Parse(path, filesProvider, alignment));
    
    private GameObject( (JsonSpriteObjectDescription description, string resourcePath, bool hasSprite, bool hasTexture) data)
    {
        Description = data.description;
        ResourcePath = data.resourcePath;
        HasSprite = data.hasSprite;
        HasTexture = data.hasTexture;
    }
    
    internal GameObject(string path, IContentManager contentManager, ContentAlign alignment): this(Parse(path, contentManager.FilesProvider, alignment))
    {
        _contentManager = contentManager;
    }
    
    public SpriteInstance CreateSpriteInstance()
    {
        if (_contentManager is null) throw new InvalidOperationException("GameObject has no content manager");
        if (!HasSprite) throw new InvalidOperationException("GameObject has no sprite");
        
        var events = Description.Events?.Select( o=>new SpriteInstance.Event(o.Sequence, o.Frame, o.Name, o.Parameters)).ToArray();
        
        return new SpriteInstance(ResourcePath, Description.Origin, events, _contentManager);
    }

    public ResourceHandle<ITexture> RequestTexture()
    {
        if (_contentManager is null) throw new InvalidOperationException("GameObject has no content manager");
        return _contentManager.Get<ITexture>(ResourcePath);
    }
    
    void IDisposable.Dispose()
    {
    }
}