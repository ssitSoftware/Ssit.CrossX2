using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class Detector(GameObjectsServices services, ObjectCreationParameters parameters)
    : SpriteGameObject(services, parameters), ISwitch
{
    void ISwitch.Toggle()
    {
    }
    
    public bool IsOn { get; private set; }
    public event Action OnChanged;

    protected void InitializePhysics(Vector2 center, SizeF size)
    {
        BoundsRect = new RectangleF(center - size.ToVector() / 2, size);
        BoundsRect = BoundsRect.Inflate(1, 1);
        
        Body.AddColliders(Body.Simulation.CreateCollider(new RectColliderCreationParameters
        {
            Center = center,
            Size = size,
            Type = ColliderType.Trigger,
            Active = true,
            AttachToBody = Body,
            Material = Material.Default
        }));
        
        Body.IsKinematic = true;
        
        AddState("Off", new State());
        AddState("On", new State());
        
        SetState("Off");
    }

    protected override void OnFixedUpdate(ref bool cancelUpdate)
    {
        base.OnFixedUpdate(ref cancelUpdate);
        
        var isOn = Body.Simulation.CheckCollision(Body.Colliders[0].Aabb, Body, IMaterial.AllColliders, 0, null, ColliderType.Dynamic);
        if (isOn != IsOn)
        {
            IsOn = isOn;
            OnChanged?.Invoke();
            SetState(IsOn ? "On" : "Off");
        }
    }
}
