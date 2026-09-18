using System.Numerics;
using Newtonsoft.Json.Linq;
using Ssit.CrossX2.Content;

namespace Ssit.CrossX2.Graphics.Sprites;

public class SpriteInstance : IDisposable
{
    public interface IHandler
    {
        void OnSpriteEvent(SpriteInstance instance, ISpriteEvent @event)
        {
        }

        void OnSequenceFinished(SpriteInstance instance, string sequenceName, bool reverse)
        {
        }
    }
    
    public class Event(string sequenceName, int frame, string eventName, JObject parameters): ISpriteEvent
    {
        internal readonly int Frame = frame;
        public string EventName { get; } = eventName;
        public string SequenceName { get; } = sequenceName;
        public TParameters GetParameters<TParameters>() where TParameters: class, new() => parameters.ToObject<TParameters>();
    }
    
    private readonly Vector2 _origin;

    private readonly ResourceHandle<Sprite> _sprite;
    private readonly ResourceHandle<ITexture> _spriteSheet;
    
    public ITexture SpriteSheet => _spriteSheet.Resource;
    
    public Vector2 Origin { get; private set; }
    public RectangleF Source { get; private set; }
    public string CurrentSequence => _currentSequence?.Name ?? "";

    private float _currentTime;
    private float _maxTime;
    private int _lastFrame = -1;
    private float _maxTimeDelta = 0.1f;

    private Sprite.SpriteSequence _currentSequence;

    private readonly Dictionary<(string, int), Event[]> _events;

    private readonly SpriteCollider _spriteCollider;
    
    public IHandler Handler { get; set; }
    
    internal SpriteInstance(ResourceHandle<ITexture> spriteSheet, Sprite sprite, Vector2 origin, IReadOnlyList<Event> events, IContentManager contentManager)
    {
        _sprite = new ResourceHandleUnmanaged<Sprite>(sprite, "");
        _spriteSheet = spriteSheet.Clone();

        _origin = origin;
        _events = PrepareEvents(events);

        _spriteCollider = TryLoadMask(sprite.SheetName, contentManager);
    }
    
    public SpriteInstance(string spritePath, Vector2 origin, IReadOnlyList<Event> events, IContentManager contentManager)
    {
        _sprite = contentManager.Get<Sprite>(spritePath);
        _spriteSheet = contentManager.Get<ITexture>(_sprite.Resource.SheetName);

        _origin = origin;
        _events = PrepareEvents(events);
        
        _spriteCollider = TryLoadMask(_sprite.Resource.SheetName, contentManager);
    }
    
    public SpriteInstance(Sprite sprite, Vector2 origin, IReadOnlyList<Event> events, IContentManager contentManager)
    {
        _sprite = new ResourceHandleUnmanaged<Sprite>(sprite, "");
        _spriteSheet = contentManager.Get<ITexture>(_sprite.Resource.SheetName);

        _origin = origin;
        _events = PrepareEvents(events);
        
        _spriteCollider = TryLoadMask(_sprite.Resource.SheetName, contentManager);
    }

    public MaskIndex CheckCollision(SpriteCollider other, Vector2 offset, Vector2 origin, RectangleF source, int scale, MaskIndex maskIndex = MaskIndex.All)
    {
        if (_spriteCollider is null)
            return MaskIndex.None;
        
        offset -= origin;
        offset += Origin;
        
        return other.CheckCollision(
            source,
            _spriteCollider, 
            Source, offset, 
            scale, 
            SpriteSheet.Size.Width / _spriteCollider.Size.Width, 
            maskIndex);
    }

    public MaskIndex CheckCollision(SpriteInstance other, Vector2 offset, MaskIndex maskIndex = MaskIndex.All)
    {
        if (other._spriteCollider is null)
            return MaskIndex.None;
        
        return CheckCollision(other._spriteCollider, offset, other.Origin, other.Source,
            other.SpriteSheet.Size.Width / other._spriteCollider.Size.Width, maskIndex);
    }

    private SpriteCollider TryLoadMask(string sheetPath, IContentManager contentManager)
    {
        var maskPath = sheetPath.Replace(".png", ".mask.png");
        if (!contentManager.FilesProvider.FileExists(maskPath.TrimEnd('!')))
            return null;
        
        return contentManager.Get<SpriteCollider>(maskPath);
    }

    private Dictionary<(string, int), Event[]> PrepareEvents(IReadOnlyList<Event> events)
    {
        if(events is null) return null;
        if(events.Count == 0) return null;
        
        var dict = new Dictionary<(string, int), List<Event>>();
        
        foreach (var @event in events)
        {
            var key = (@event.SequenceName, @event.Frame);
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<Event>();
                dict.Add(key, list);
            }
            list.Add(@event);
        }

        return new Dictionary<(string, int), Event[]>(dict.Select(x =>
            new KeyValuePair<(string, int), Event[]>(x.Key, x.Value.ToArray())));
    }

    public void SetSequence(string sequenceName, bool resetPosition = false)
    {
        if (_currentSequence?.Name == sequenceName)
        {
            if (resetPosition)
            {
                _currentTime = 0;
                _lastFrame = -1;
            }
            return;
        }

        _currentSequence = _sprite.Resource.GetSequence(sequenceName);
        
        _currentTime = 0;
        _lastFrame = -1;
        _maxTime = 0;
        _maxTimeDelta = 0.5f;

        foreach (var frame in _currentSequence.Frames)
        {
            _maxTime += frame.Duration;
            _maxTimeDelta = MathF.Min(_maxTimeDelta, frame.Duration);
        }
        
        AdvanceOne(0, false);

        Source = _currentSequence.Frames[_lastFrame].Source;
        Origin = _currentSequence.Frames[_lastFrame].Offset + _origin;
    }

    public void Advance(float dt, bool reverse = false)
    {
        if (_currentSequence is null)
        {
            Source = Rectangle.Empty;
            Origin = Vector2.Zero;
            return;
        }

        while (dt > 0)
        {
            var delta = MathF.Min(dt, _maxTimeDelta);
            dt -= delta;
            AdvanceOne(delta, reverse);
        }

        Source = _currentSequence.Frames[_lastFrame].Source;
        Origin = _currentSequence.Frames[_lastFrame].Offset + _origin;
    }

    private void AdvanceOne(float delta, bool reverse)
    {
        _currentTime += delta * (reverse ? -1 : 1);
        var currentSequence = _currentSequence.Name;

        if (_currentTime >= _maxTime || _currentTime < 0)
        {
            Handler?.OnSequenceFinished(this, _currentSequence.Name, _currentTime < 0);
            if (_currentSequence?.Name != currentSequence || _currentSequence is null)
            {
                return;
            }
        }

        _currentTime += _maxTime;
        _currentTime %= _maxTime;

        var time = _currentTime;
        var frame = 0;

        while (time >= _currentSequence.Frames[frame].Duration)
        {
            time -= _currentSequence.Frames[frame].Duration;
            frame++;
        }

        if (frame != _lastFrame)
        {
            if(_events != null && _events.TryGetValue((currentSequence, frame), out var events))
            {
                foreach (var @event in events)
                {
                    Handler?.OnSpriteEvent(this, @event);
                }
            }
        }

        _lastFrame = frame;
    }

    public void Dispose()
    {
        _spriteSheet?.Dispose();
        _sprite?.Dispose();
    }
}