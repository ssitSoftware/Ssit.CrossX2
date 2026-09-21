using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class SpriteGameObject2 : IGameObjectRenderer, IBodyOwner
{
    public GameObjectsServices Services { get; }
    public IBody Body { get; }
    public event Action FixedUpdate;
    public int ZOrder { get; protected set; }
    
    public SpriteInstance Sprite { get; private set; }
    private ResourceHandle<SpriteEx> _spriteObject;
    
    protected ImageTransform Transform { get; set; }
    
    protected RectangleF BoundsRect { get; set; }

    private readonly List<IUpdatable> _updatables = new();
    public RectangleF Bounds => BoundsRect.Offset(Body.Position);
    
    public virtual bool FaceLeft
    {
        get => Transform == ImageTransform.FlipHorizontal;
        set => Transform = value ? ImageTransform.FlipHorizontal : ImageTransform.None;
    }
    
    void IGameObjectRenderer.Render(IRenderer renderer, RgbaColor color) => OnRender(renderer, color);

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

    protected SpriteGameObject2(GameObjectsServices services, ObjectCreationParameters parameters)
    {
        ZOrder = parameters.ZOrder;
        
        Services = services;

        Body = services.Simulation.CreateBody(this);

        Body.Touch();
        Body.Position = parameters.Position;
        
        Transform = parameters.Flipped ? ImageTransform.FlipHorizontal : ImageTransform.None;
    }
    
    protected void InitializeSprite(string spritePath)
    {
        _spriteObject = Services.ContentManager.Get<SpriteEx>(spritePath);
        Sprite = _spriteObject.Resource.CreateSpriteInstance();
    }
    
    protected virtual void OnSpriteEvent(ISpriteEvent @event)
    {
    }
    
    protected virtual void OnSequenceFinished(string sequenceName)
    {
    }

    protected virtual void OnRender(IRenderer renderer, RgbaColor color)
    {
        if (Sprite is null)
            return;

        var position = Body.Position;
        if (this is IPositionObject positionObject)
        {
            position = positionObject.Position;
        }

        position *= Services.GameTemplate.TileSize;
        renderer.SpriteRenderer.Draw(Sprite, position, transform: Transform, color: color);
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
}