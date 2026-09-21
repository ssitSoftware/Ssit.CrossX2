using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class MechanicalDoor(GameObjectsServices services, ObjectCreationParameters<MechanicalDoor.Parameters> parameters)
    : SpriteGameObject(services, parameters)
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Parameters
    {
        [EditorLink(typeof(ISwitch))] public int Switch { get; set; }
        [Editor] public bool Inverse { get; set; }
    }
    
    private ISwitch _switch;
    private bool _inverse;
    
    protected bool IsOpen { get; private set; }
    private ICollider _staticCollider;

    protected void InitializePhysics(ObjectCreationParameters<Parameters> parameters, Vector2 offset, SizeF size, IMaterial material, float topHandleHeight = 0)
    {
        BoundsRect = new RectangleF(-size.Width, -size.Height, size.Width * 2, size.Height * 2);
        BoundsRect = BoundsRect.Inflate(1, 1);
        
        Body.AddColliders(Body.Simulation.CreateCollider(new RectColliderCreationParameters
        {
            Active = true,
            AttachToBody = Body,
            Center = new Vector2(0, -size.Height / 2),
            Size = size,
            Type = ColliderType.Static,
            Material = material
        }));

        if (topHandleHeight > 0)
        {
            _staticCollider = Body.Simulation.CreateCollider(new RectColliderCreationParameters
            {
                Active = true,
                AttachToBody = Body,
                Center = new Vector2(0, -size.Height + topHandleHeight / 2),
                Size = new Vector2(size.Width, topHandleHeight),
                Type = ColliderType.Static,
                Material = Material.Default
            });
            Body.AddColliders(_staticCollider);
        }

        Body.IsKinematic = true;
        
        AddState("Opening", null);
        AddState("Open", null);
        AddState("Closed", null);
        AddState("Closing", null);
        
        SetState("Closed");
        
        _inverse = parameters.Parameters.Inverse;
        
        parameters.LinkMap.RequestLink<ISwitch>(parameters.Parameters.Switch, s =>
        {
            _switch = s;
            IsOpen = (_switch?.IsOn ?? false) ^ _inverse;
            UpdateState();
        });
    }

    private void UpdateState()
    {
        SetState(IsOpen ? "Open" : "Closed");
        Body.Colliders[0].IsActive = !IsOpen;

        foreach (var col in Body.Simulation.GetColliders(Body.Colliders[0].Aabb, Body, IMaterial.AllColliders, 0, ColliderType.Dynamic))
        {
            col.AttachedBody?.Touch();
        }
    }
    
    protected override void OnFixedUpdate(ref bool cancelUpdate)
    {
        base.OnFixedUpdate(ref cancelUpdate);

        if (CurrentState is "Opening" or "Closing")
            return;
        
        var open = (_switch?.IsOn ?? false) ^ _inverse;

        if (open)
        {
            if (!IsOpen)
            {
                SetState("Opening");
            }
        }
        else
        {
            if (IsOpen)
            {
                SetState("Closing");
            }
        }
    }

    protected override void OnAnimationFinished(string sequenceName)
    {
        base.OnAnimationFinished(sequenceName);

        switch (sequenceName)
        {
            case "Opening":
                IsOpen = true;
                UpdateState();
                break;
            
            case "Closing":
                IsOpen = false;
                UpdateState();
                break;
        }
    }
}