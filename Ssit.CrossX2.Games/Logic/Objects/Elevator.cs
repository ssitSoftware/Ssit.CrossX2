using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Behaviors;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class Elevator(GameObjectsServices services, ObjectCreationParameters<Elevator.Parameters> parameters)
    : SpriteGameObject(services, parameters), ITarget
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Parameters
    {
        [EditorInt(2,8)] public int Speed { get; set; } = 5;
        [EditorFloat(0.0f,10f, 0.1f)] public float BrakingDistance { get; set; } = 3f;
        [EditorLink(typeof(ITarget))] public int Target { get; set; }
        [EditorLink(typeof(ISwitch))] public int Switch { get; set; }
    }

    Vector2 ITarget.Position => _initialPosition;
    ITarget ITarget.Next => _target;
    
    private ITarget _target;
    private ISwitch _switch;
    private Vector2 _initialPosition;
    
    public bool IsOn => _switch?.IsOn ?? true;
    public ITarget CurrentTarget { get; set; }
    public float Speed { get; private set; }
    public float BrakingDistance { get; private set;}

    protected void InitializePhysics(ObjectCreationParameters<Parameters> parameters, float width, IMaterial material)
    {
        BoundsRect = new RectangleF(-width, -width/2, width * 2, width);

        var speed = parameters.Parameters.Speed;
        Speed = speed;
        BrakingDistance = parameters.Parameters.BrakingDistance;
        Body.IsKinematic = true;
        Body.Mass = 10000;
        
        Body.AddColliders(Body.Simulation.CreateCollider(new RectColliderCreationParameters
        {
            AttachToBody = Body,
            Active = true,
            Center = new Vector2(0, 0.1f),
            Size = new Vector2(width, 0.2f),
            Type = ColliderType.Dynamic,
            Material = material
        }));
        
        _initialPosition = parameters.Position;
        
        parameters.LinkMap.RequestLink<ITarget>(parameters.Parameters.Target, t => CurrentTarget = _target = t);
        parameters.LinkMap.RequestLink<ISwitch>(parameters.Parameters.Switch, s => _switch = s);
        
        var behavior = new ElevatorBehavior(this);
        
        AddState("Off", new State(behavior));
        AddState("On", new State(behavior));
        
        SetState("Off");
    }
}