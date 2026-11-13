using System.Numerics;
using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class SpriteGameObject : IGameObjectRenderer, IBodyOwner, ISoundEmitter
{
    public GameObjectsServices Services { get; }
    public IBody Body { get; }
    
    /// <summary>
    /// Id of the object in the map file. 0 for objects created at runtime.
    /// </summary>
    public int MapId { get; }
    public event Action FixedUpdate;
    public int ZOrder { get; protected set; }
    
    public SpriteInstance Sprite { get; private set; }
    private ResourceHandle<SpriteEx> _spriteObject;
    
    protected ImageTransform Transform { get; set; }
    
    protected RectangleF BoundsRect { get; set; }

    private readonly List<IUpdatable> _updatables = new();
    private readonly Dictionary<Type, object> _parameters = new();
    public RectangleF Bounds => BoundsRect.Offset(Body.Position);
    
    public virtual bool FaceLeft
    {
        get => Transform == ImageTransform.FlipHorizontal;
        set => Transform = value ? ImageTransform.FlipHorizontal : ImageTransform.None;
    }
    
    void IGameObjectRenderer.Render(IRenderer renderer, RgbaColor color, float depth) => OnRender(renderer, color, depth);

    void IBodyOwner.OnFixedUpdate(out bool cancelUpdate)
    {
        cancelUpdate = false;
        OnFixedUpdate(ref cancelUpdate);

        var dt = Services.Simulation.SimulationParameters.TimeDelta;
        
        foreach (var updatable in _updatables)
        {
            updatable.FixedUpdate(dt);
        }

        FixedUpdate?.Invoke();
    }

    void IBodyOwner.OnPostFixedUpdate()
    {
        OnPostFixedUpdate();
        
        foreach (var updatable in _updatables)
        {
            updatable.PostFixedUpdate();
        }
    }

    void IBodyOwner.OnUpdate(float time)
    {
        OnUpdate(time);
        
        foreach (var updatable in _updatables)
        {
            updatable.Update(time);
        }
    }
    
    void IBodyOwner.OnPostUpdate()
    {
        OnPostUpdate();
        
        foreach (var updatable in _updatables)
        {
            updatable.PostUpdate();
        }
    }
    
    internal void CallSpriteEvent(ISpriteEvent @event) => OnSpriteEvent(@event);
    internal void CallSequenceFinished(string sequenceName) => OnSequenceFinished(sequenceName);

    internal void AddUpdatableInternal(IUpdatable updatable) => _updatables.Add(updatable);
    protected void AddUpdatable(IUpdatable updatable) => _updatables.Add(updatable);

    protected SpriteGameObject(GameObjectsServices services)
    {
        Services = services;
        Body = services.Simulation.CreateBody(this);
        Body.Touch();
    }
    
    protected SpriteGameObject(GameObjectsServices services, ObjectCreationParameters parameters)
    {
        ZOrder = parameters.ZOrder;
        MapId = parameters.MapId;
        
        Services = services;

        Body = services.Simulation.CreateBody(this);

        Body.Touch();
        Body.Position = parameters.Position;
        
        Transform = parameters.Flipped ? ImageTransform.FlipHorizontal : ImageTransform.None;
    }
    
    public TParameters GetParameters<TParameters>()
    {
        if (!_parameters.TryGetValue(typeof(TParameters), out var obj))
        {
            obj = Activator.CreateInstance<TParameters>();
            _parameters.Add(typeof(TParameters), obj);
        }
        return (TParameters)obj;
    }

    /// <summary>
    /// Removes the parameters object of the given type - the next <see cref="GetParameters{TParameters}"/> creates a new instance.
    /// </summary>
    /// <returns>True if the parameters existed.</returns>
    public bool RemoveParameters<TParameters>() => _parameters.Remove(typeof(TParameters));

    /// <summary>
    /// Removes all parameters objects - every <see cref="GetParameters{TParameters}"/> creates a new instance afterward.
    /// </summary>
    public void ClearParameters() => _parameters.Clear();

    protected void InitializeSprite(string spritePath, bool useCache = true)
    {
        if (useCache)
        {
            _spriteObject = new ResourceHandleUnmanaged<SpriteEx>(Services.ContentCache.Get<SpriteEx>(spritePath), spritePath, false);
            Sprite = _spriteObject.Resource.CreateSpriteInstance(Services.ContentCache);
        }
        else
        {
            _spriteObject = Services.ContentManager.Get<SpriteEx>(spritePath);
            Sprite = _spriteObject.Resource.CreateSpriteInstance();
        }
    }
    
    protected virtual void OnSpriteEvent(ISpriteEvent @event)
    {
    }
    
    protected virtual void OnSequenceFinished(string sequenceName)
    {
    }

    protected virtual void OnRender(IRenderer renderer, RgbaColor color, float depth)
    {
        if (Sprite is null)
            return;

        var position = Body.Position;
        if (this is IPositionObject positionObject)
        {
            position = positionObject.Position;
        }

        position *= Services.GameTemplate.TileSize;
        renderer.SpriteRenderer.Draw(Sprite, position, transform: Transform, color: color, depth: depth);
    }

    protected virtual void OnFixedUpdate(ref bool cancelUpdate)
    {
        var dt = Services.Simulation.SimulationParameters.TimeDelta;
        Sprite?.Advance(dt);
    }

    protected virtual void OnPostFixedUpdate()
    {
    }
    
    protected virtual void OnUpdate(float dt)
    {
    }

    protected virtual void OnPostUpdate()
    {
        
    }
    
    void IDisposable.Dispose()
    {
        OnDispose(true);
    }

    protected virtual void OnDispose(bool disposing)
    {
        if (Sprite is not null)
        {
            Sprite.Handler = null;
            Sprite.Dispose();
            Sprite = null;
        }
        
        _spriteObject?.Dispose();
        _spriteObject = null;
    }

    Vector2 ISoundEmitter.Position => Body.Colliders[0].Aabb.Center;
}