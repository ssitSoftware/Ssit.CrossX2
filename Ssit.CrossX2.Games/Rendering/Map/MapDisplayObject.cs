using System.Numerics;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Rendering.Map;

public class MapDisplayObject: IDisposable
{
    private readonly ResourceHandle<ITexture> _texture;

    private readonly ResourceHandle<GameObject> _gameObject;
    
    public SpriteInstance SpriteInstance { get; }

    public ITexture Texture => _texture?.Resource;
    public Vector2 Origin { get; }
    public bool IsFlipped { get; }
    public Vector2 Position { get; }
    public int Zorder { get; internal set; }
    public string Name => _gameObject.Name;

    public MapDisplayObject(IContentManager contentManager, IGameTemplate template, MapObject obj)
    {
        IsFlipped = obj.Flipped;

        var imgInfo = template.Images.First(o => o.FullName == obj.TypeId);
        
        _gameObject = contentManager.Get<GameObject>(imgInfo.GameObject);

        if (_gameObject.Resource.HasSprite)
        {
            SpriteInstance = _gameObject.Resource.CreateSpriteInstance();
            SpriteInstance.SetSequence(imgInfo.Sequence);
        }
        else
        {
            _texture = _gameObject.Resource.RequestTexture();
            Origin = _gameObject.Resource.Description.Origin;
        }
        
        if (obj.ParametersObject is StaticObjectParameters sp)
        {
            var timeOffset = sp.AnimationTimeOffsetInMs / 1000f;
            SpriteInstance?.Advance(timeOffset);
        }
        
        Position = obj.Position * template.TileSize;
    }

    public void Dispose()
    {
        _gameObject?.Dispose();
        SpriteInstance?.Dispose();
        _texture?.Dispose();
    }

    public void Update(float dt)
    {
        SpriteInstance?.Advance(dt);
    }
}