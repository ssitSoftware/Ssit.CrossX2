using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class SpriteGameObject: StateGameObject, IGameObjectRenderer, SpriteInstance.IHandler, IBodyOwner
{
    public GameObjectsServices Services { get; }
    public IBody Body { get; }
    public SpriteInstance Sprite { get; private set; }
    private ResourceHandle<SpriteEx> _spriteObject; 

    public int ZOrder { get; }
    
    protected ImageTransform Transform { get; set; }
    
    protected RectangleF BoundsRect { get; set; }
    
    public RectangleF Bounds => BoundsRect.Offset(Body.Position);

    void IGameObjectRenderer.Render(IRenderer renderer, RgbaColor color) => OnRender(renderer, color);

    void IBodyOwner.OnFixedUpdate(out bool cancelUpdate)
    {
        cancelUpdate = false;
        OnFixedUpdate(ref cancelUpdate);
    }
    
    void IBodyOwner.OnPostFixedUpdate() => OnPostFixedUpdate();
    void IBodyOwner.OnUpdate(float dt) => OnUpdate(dt);
    
    public bool FaceLeft
    {
        get => Transform == ImageTransform.FlipHorizontal;
        set => Transform = value ? ImageTransform.FlipHorizontal : ImageTransform.None;
    }
    
    protected SpriteGameObject(GameObjectsServices services, ObjectCreationParameters parameters)
    {
        ZOrder = parameters.ZOrder;
        
        Services = services;
        
        Body =  services.Simulation.CreateBody(this);
        Body.Touch();
        Body.Position = parameters.Position;
        
        Transform = parameters.Flipped ? ImageTransform.FlipHorizontal : ImageTransform.None;
    }

    protected void InitializeSprite(string spritePath)
    {
        _spriteObject = Services.ContentManager.Get<SpriteEx>(spritePath);
        Sprite = _spriteObject.Resource.CreateSpriteInstance();
        Sprite.Handler = this;
    }

    protected virtual void OnSpriteEvent(SpriteInstance instance, ISpriteEvent @event)
    {
    }

    protected virtual void OnRender(IRenderer renderer, RgbaColor color)
    {
        var pos = Body.Position * Services.GameTemplate.TileSize;
        renderer.SpriteRenderer.Draw(Sprite, pos, transform: Transform, color: color);
    }

    protected virtual void OnFixedUpdate(ref bool cancelUpdate)
    {
        var dt = Services.Simulation.SimulationParameters.TimeDelta;
        OnFixedUpdate(dt);
        Sprite.Advance(dt);
    }

    protected override void SetSequence(string state)
    {
        Sprite.SetSequence(state);
    }
    
    void IDisposable.Dispose()
    {
        OnDispose(true);
    }

    protected virtual void OnDispose(bool disposing)
    {
        Sprite?.Dispose();
        Sprite = null;

        _spriteObject?.Dispose();
        _spriteObject = null;
    }

    void SpriteInstance.IHandler.OnSpriteEvent(SpriteInstance instance, ISpriteEvent @event) => OnSpriteEvent(instance, @event);

    void SpriteInstance.IHandler.OnSequenceFinished(SpriteInstance instance, string sequenceName, bool reverse) => OnAnimationFinished(sequenceName);
}