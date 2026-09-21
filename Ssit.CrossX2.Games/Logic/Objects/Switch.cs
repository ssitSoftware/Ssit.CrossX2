using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class Switch(GameObjectsServices services, ObjectCreationParameters<Switch.Parameters> parameters)
    : SpriteGameObject(services, parameters), ISwitch, ILogicOperable
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Parameters
    {
        [Editor] public bool IsOn { get; set; }
        [EditorLink(typeof(ISwitch))] public int ToggleAnother { get; set; }
    }
    
    void ILogicOperable.Operate(IBodyOwner @operator) => Toggle();

    public event Action OnChanged;
    
    public bool IsOn { get; private set; }
    
    private ISwitch _anotherToggle;
    private bool _firstUpdate = true;
    
    protected void InitializePhysics(ObjectCreationParameters<Parameters> parameters, float detectorRadius, Vector2 detectorOffset)
    {
        BoundsRect = new RectangleF(-detectorRadius * 2, -detectorRadius * 2, detectorRadius * 4, detectorRadius * 4);
        BoundsRect = BoundsRect.Inflate(1, 1);
        
        Body.AddColliders(Body.Simulation.CreateCollider(new RectColliderCreationParameters
        {
            Size = new SizeF(detectorRadius * 2, detectorRadius * 2),
            Center = detectorOffset,
            Type = ColliderType.Trigger,
            Active = true,
            AttachToBody = Body,
            Material = Material.Default
        }));
        Body.IsKinematic = true;

        AddState("On", new State());
        AddState("Off", new State());
        AddState("TurnOff", new State());
        AddState("TurnOn", new State());
        
        SetState("Off");
        
        IsOn = parameters.Parameters.IsOn;

        if (parameters.Parameters.ToggleAnother != 0)
        {
            parameters.LinkMap.RequestLink<ISwitch>(parameters.Parameters.ToggleAnother, UpdateAnotherToggle);
        }
    }

    protected override void OnFixedUpdate(ref bool cancelUpdate)
    {
        base.OnFixedUpdate(ref cancelUpdate);

        if (_firstUpdate)
        {
            _firstUpdate = false;
            UpdateState();
            OnChanged?.Invoke();
        }
    }

    private void UpdateAnotherToggle(ISwitch another)
    {
        _anotherToggle = another;
        _anotherToggle.OnChanged += UpdateState;
    }

    private void UpdateState()
    {
        var wasOn = CurrentState  is "On" or "TurnOn";
        IsOn = _anotherToggle?.IsOn ?? IsOn;

        if (wasOn != IsOn)
        {
            SetState(IsOn ?  "TurnOn" : "TurnOff");
            OnChanged?.Invoke();
        }
    }

    protected override void OnAnimationFinished(string sequenceName)
    {
        base.OnAnimationFinished(sequenceName);
        switch (sequenceName)
        {
            case "TurnOn":
                SetState("On");
                break;
            
            case "TurnOff":
                SetState("Off");
                break;
        }
    }

    public virtual void Toggle()
    {
        if (_anotherToggle != null)
        {
            _anotherToggle.Toggle();
        }
        else
        {
            IsOn = !IsOn;
        }
        UpdateState();
        OnChanged?.Invoke();
    }
}