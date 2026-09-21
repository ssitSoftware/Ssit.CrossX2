using System.Numerics;
using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Games.Audio;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public abstract class CharacterObject<TCharacter> : SpriteGameObject2, IBodyEventsReceiver, IActivationHandler, ISteeringCharacter, IPositionObject where TCharacter: CharacterObject<TCharacter>
{
    TParameters IGameObject.Get<TParameters>(bool create) => GetParameters<TParameters>(create);
    SteeringState<ISteeringCharacter> ISteeringCharacter.CurrentSteeringState => SteeringStateMachine.InternalStateMachine.CurrentState;
    
    ISteeringInput ISteeringCharacter.SteeringInput => SteeringInput;
    
    public CharacterSteeringParameters SteeringParameters { get; } = new();
    
    protected abstract ISteeringInput SteeringInput { get; }
    
    public virtual Vector2 Position => Body.Position;
    
    public Vector2 MomentumOffset { get; set; }
    protected Vector2 GroundDetectionEpsilon { get; set; } = Vector2.Zero;
    
    public ICharacterPhysicsValues PhysicsValues { get; protected init; }
    public ICommonSoundContainer CommonSoundContainer { get; }
    protected ContextSoundContainer SoundContainer { get; init; }

    protected SpriteGameObjectStateMachine<TCharacter, ISteeringCharacter> SteeringStateMachine { get; private set; }

    private readonly Dictionary<Type, object> _parameters = new();

    TParameters ICharacter.GetParameters<TParameters>(bool create) => GetParameters<TParameters>(create);
    
    protected TParameters GetParameters<TParameters>(bool create)
    {
        if (!_parameters.TryGetValue(typeof(TParameters), out var obj))
        {
            if (create)
            {
                obj = Activator.CreateInstance<TParameters>();
                _parameters.Add(typeof(TParameters), obj);
            }
            else throw new Exception("No parameters of given type.");
        }
        return (TParameters)obj;
    }
    
    void IBodyEventsReceiver.OnCollision(ICollider source, ICollider other, Vector2 impact)
    {
        SteeringStateMachine.InternalStateMachine.CurrentState?.Collission((TCharacter)this, source, other, impact);
    }
    
    public void SetSteeringState(string name) => SteeringStateMachine.SetSteeringState(name);
    
    ContextSoundContainer IGameObject.SoundContainer => SoundContainer;
    
    protected CharacterObject(GameObjectsServices services, ObjectCreationParameters parameters): base(services, parameters)
    {
        CommonSoundContainer = services.CommonSoundContainer;
        Body.AddEventsReceiver(this);
    }
    
    protected void InitializeSteeringStateMachine()
    {
        SteeringStateMachine = new SpriteGameObjectStateMachine<TCharacter, ISteeringCharacter>((TCharacter)this);
        SteeringStateMachine.InternalStateMachine.OnStateChanged += SteeringStateMachineOnOnStateChanged;
    }
    
    protected virtual void DetectOnGround()
    {
        this.DetectOnGround(out SteeringParameters.IsOnGround, out SteeringParameters.IsOnPlatform, out SteeringParameters.IsOnStaticGround, out SteeringParameters.GroundMaterial, GroundDetectionEpsilon);
    }
    
    private static void SteeringStateMachineOnOnStateChanged(object sender, SteeringState<ISteeringCharacter> _)
    {
        var sm = (SteeringStateMachine<ISteeringCharacter>)sender;
        ((CharacterObject<TCharacter>)sm.Object).DetectOnGround();
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);
        
        Body.RemoveEventsReceiver(this);
        
        SteeringStateMachine.InternalStateMachine.OnStateChanged -= SteeringStateMachineOnOnStateChanged;
        SoundContainer?.Dispose();
    }

    void IActivationHandler.Activate(bool active)
    {
        if (!active && SoundContainer is not null)
        {
            SoundContainer.StopAll();
        }
    }
}