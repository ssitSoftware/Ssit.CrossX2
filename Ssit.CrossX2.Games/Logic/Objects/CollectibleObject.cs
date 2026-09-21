using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class CollectibleObject : SpriteGameObject2, ICollectible, SpriteInstance.IHandler
{
    private readonly string _idleSequence;
    private readonly string _soundId;
    private readonly ICommonSoundContainer _soundContainer;

    protected CollectibleObject(string spritePath, string idleSequence, string soundId, GameObjectsServices services, ObjectCreationParameters parameters, SizeF size = default)
        : base(services, parameters)
    {
        _idleSequence = idleSequence;
        _soundId = soundId;

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

    bool ICollectible.Collect()
    {
        if (!Body.Colliders[0].IsActive)
            return false;

        Body.Colliders[0].IsActive = false;
        Sprite.SetSequence($"{_idleSequence} Collect");

        _soundContainer.Play(_soundId);
        OnCollect();
        return true;
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
