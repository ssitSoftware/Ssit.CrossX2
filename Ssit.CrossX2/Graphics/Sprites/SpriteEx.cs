using System.Numerics;
using Ssit.CrossX2.Content;
using Ssit.CrossX2.Graphics.Sprites.Json;
using Ssit.CrossX2.IO;
using Ssit.CrossX2.IoC;

namespace Ssit.CrossX2.Graphics.Sprites;

public class SpriteEx: IDisposable
{
    public class Parameters
    {
        public string ImagePath { get; init; }
        public Vector2 Origin { get; init; }
        public Size FrameSize { get; init; }
        public int Fps { get; init; } = 15;
        public (string sequence, int start, int len)[] Sequences { get; init; }
    }
    
    private readonly IContentManager _contentManager;
    public readonly Sprite Sprite;
    public readonly JsonSpriteObjectDescription Description;

    private ResourceHandle<ITexture> _spriteSheet;
    
    public static IDisposable Load(string path, IFilesProvider filesProvider, IContentManager contentManager, IIoCContainer iocContainer)
    {
        return LoadInternal(path, filesProvider, contentManager, iocContainer);
    }

    private static SpriteEx LoadInternal(string path, IFilesProvider filesProvider, IContentManager contentManager, IIoCContainer iocContainer)
    {
        ContentAlign originAlignment;
        if (iocContainer.TryGet<IDefaultSpriteConfiguration>(out var config))
        {
            originAlignment = config.OriginAlignment;
        }
        else
        {
            originAlignment = ContentAlign.Center | ContentAlign.VCenter;
        }
        
        var addedPersistent = path.EndsWith('!') ? "!" : "";
        
        var sprite = JsonSpriteLoader.LoadInternal(path +  ".json" + addedPersistent, filesProvider);
        
        JsonSpriteObjectDescription desc;
        Size sourceSize = Size.Zero;
        
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
        
        return new SpriteEx(sprite, desc, contentManager);
    }

    public SpriteEx(IContentManager contentManager, Parameters parameters)
    {
        _spriteSheet = contentManager.Get<ITexture>(parameters.ImagePath);
        _contentManager = contentManager;
        
        Description = new JsonSpriteObjectDescription(parameters.Origin, parameters.FrameSize);

        var sequences = new List<Sprite.SpriteSequence>();

        var frameDuration = 1f / parameters.Fps;
        foreach (var seq in parameters.Sequences)
        {
            var frames = new List<Sprite.SpriteFrame>();
            for (var idx = 0; idx < seq.len; idx++)
            {
                var columns = _spriteSheet.Resource.Size.Width /  parameters.FrameSize.Width;
                
                var x = (seq.start + idx) % columns;
                var y = (seq.start + idx) / columns;
                
                var rect = new Rectangle(x * parameters.FrameSize.Width, y *  parameters.FrameSize.Height, parameters.FrameSize.Width, parameters.FrameSize.Height);
                var frame = new Sprite.SpriteFrame(rect, Vector2.Zero, frameDuration);
                
                frames.Add(frame);
            }
            
            var sequence = new Sprite.SpriteSequence(seq.sequence, frames.ToArray());
            sequences.Add(sequence);
        }
        
        Sprite = new Sprite(parameters.ImagePath, sequences, parameters.FrameSize);
    }
    
    private SpriteEx(Sprite sprite, JsonSpriteObjectDescription description, IContentManager contentManager)
    {
        _contentManager = contentManager;
        Sprite = sprite;
        Description = description;
    }

    public SpriteInstance CreateSpriteInstance()
    {
        var events = Description.Events?.Select( o => new SpriteInstance.Event(o.Sequence, o.Frame, o.Name, o.Parameters)).ToArray();

        if (_spriteSheet != null)
        {
            return new SpriteInstance(_spriteSheet, Sprite, Description.Origin, events, _contentManager);
        }
        
        return new SpriteInstance(Sprite, Description.Origin, events, _contentManager);
    }

    void IDisposable.Dispose()
    {
        _spriteSheet?.Dispose();
        _spriteSheet = null;
    }
}