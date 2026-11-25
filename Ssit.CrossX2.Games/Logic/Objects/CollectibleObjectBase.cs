using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class CollectibleObjectBase : SpriteGameObject, ICollectible, SpriteInstance.IHandler
{
    private readonly string _idleSequence;
    private readonly string _soundId;
    private readonly ICommonSoundContainer _soundContainer;

    /// <summary>Game-defined collectible type, returned by <see cref="ICollectible.Collect"/>.</summary>
    public byte CollectibleType { get; }

    /// <summary>Not collected yet (the trigger is still active).</summary>
    public bool CanBeCollected => Body.Colliders[0].IsActive;

    protected CollectibleObjectBase(string spritePath, string idleSequence, string soundId, GameObjectsServices services, ObjectCreationParameters parameters, SizeF size = default, byte collectibleType = 0)
        : base(services, parameters)
    {
        _idleSequence = idleSequence;
        _soundId = soundId;
        CollectibleType = collectibleType;

        _soundContainer = services.CommonSoundContainer;

        var mainCollider = services.Simulation.CreateCollider(new RectColliderCreationParameters
        {
            Type = ColliderType.Trigger,
            AttachToBody = Body,
            Active = true,
            Size = size.Width == 0 ? new SizeF(0.4f, 0.4f) : size,
            Material = Material.Default
        });

        Body.AddColliders(mainCollider);
        Body.IsKinematic = true;

        InitializeSprite(spritePath);
        Sprite.Handler = this;
        Sprite.SetSequence(_idleSequence);
        Sprite.Advance(Random.Shared.NextSingle() * 10);
    }

    byte ICollectible.Collect()
    {
        // Collected already - plays the collect animation only once.
        if (!CanBeCollected)
            return CollectibleType;

        Body.Colliders[0].IsActive = false;
        Sprite.SetSequence($"{_idleSequence} Collect");

        _soundContainer.Play(_soundId);
        OnCollect();
        return CollectibleType;
    }

    protected virtual void OnCollect()
    {
    }

    public void OnSpriteEvent(SpriteInstance instance, ISpriteEvent @event)
    {
    }

    public void OnSequenceFinished(SpriteInstance instance, string sequenceName, bool reverse)
    {
        if (sequenceName.EndsWith("Collect"))
        {
            Body.Simulation.RemoveBody(Body);
        }
    }
}
