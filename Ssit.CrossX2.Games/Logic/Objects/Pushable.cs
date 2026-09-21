using System.Numerics;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public class Pushable(GameObjectsServices services, ObjectCreationParameters parameters): IGameObjectRenderer, IBodyOwner, IPushable
{
    protected virtual bool CanPull => false;
    bool IPushable.CanPull => CanPull;
    
    void IGameObjectRenderer.Render(IRenderer renderer, RgbaColor color) => Render(renderer, color);
    RectangleF IGameObjectRenderer.Bounds => BoundsRect.Offset(Body.Position);
    int IGameObjectRenderer.ZOrder { get; } = parameters.ZOrder;
    
    void IBodyOwner.OnFixedUpdate(out bool cancelUpdate)
    {
        cancelUpdate = false;
        FixedUpdate?.Invoke();
    }
    
    protected GameObjectsServices Services { get; } = services;
    
    public event Action FixedUpdate;
    public IBody Body { get; private set; }
    protected RectangleF BoundsRect { get; set; }
    
    private ResourceHandle<ITexture> _spriteSheet;
    private Sprite.SpriteSequence _sequence;
    
    protected Vector2 Origin { get; private set; }
    
    protected void InitializeSprite(string spritePath)
    {
        using var go = Services.ContentManager.Get<SpriteEx>(spritePath);
        var sprite = go.Resource.Sprite;

        Origin = go.Resource.Description.Origin;
        _spriteSheet = Services.ContentManager.Get<ITexture>(sprite.SheetName);
        _sequence = sprite.GetSequence("Default");
    }
    
    protected void InitializePhysics(SizeF size, IMaterial material)
    {
        var simulation = Services.Simulation;
        
        Body = simulation.CreateBody(this);
        Body.Position  = parameters.Position;
        
        Body.AddColliders(simulation.CreateCollider(new RectColliderCreationParameters
        {
            Active = true,
            AttachToBody = Body,
            Center = Vector2.Zero,
            Size = size,
            Type = ColliderType.Dynamic,
            Material = material
        }));

        BoundsRect = (RectangleF)Body.Colliders[0].GetAabb(Vector2.Zero);
        BoundsRect = BoundsRect.Inflate(1, 1);
    }
    
    protected virtual void Render(IRenderer renderer, RgbaColor color)
    {
        var pos = Body.Position * Services.GameTemplate.TileSize;
        
        renderer.SpriteRenderer.Draw(_spriteSheet.Resource, pos, 
            _sequence.Frames[0].Source, 
            _sequence.Frames[0].Offset + Origin, 0, 1f, color);
    }

    public void Dispose()
    {
        _spriteSheet?.Dispose();
        _spriteSheet = null;
    }
}