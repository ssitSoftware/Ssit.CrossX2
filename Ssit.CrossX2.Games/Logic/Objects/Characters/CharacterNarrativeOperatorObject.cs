using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Narration;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public abstract class CharacterNarrativeOperatorObject<TCharacter> : CharacterObject<TCharacter>, IStoryOperator
    where TCharacter : CharacterNarrativeOperatorObject<TCharacter>
{
    private readonly IActionScheduler _actionScheduler;
    private readonly ICamera _camera;
    private readonly INarrationSystem _narrationSystem;
    private readonly IGameState _gameState;

    private float? _walkToPositionX;
    private TaskCompletionSource _walkToTaskCompletionSource;

    protected CharacterNarrativeOperatorObject(GameObjectsServices services, ObjectCreationParameters parameters,
        IActionScheduler actionScheduler, ICamera camera, INarrationSystem narrationSystem, IGameState gameState)
        : base(services, parameters)
    {
        _actionScheduler = actionScheduler;
        _camera = camera;
        _narrationSystem = narrationSystem;
        _gameState = gameState;

        _gameState = gameState;
        _gameState.StateUpdated += OnGameStateUpdated;
    }
    
    bool IStoryOperator.ExecuteStoryConversation(INpcCharacter npc, string conversationId)
    {
        if (!SteeringParameters.IsOnGround) 
            return false;
        
        if (npc is null)
        {
            TalkToSelf(conversationId);
        }
        
        ExecuteStoryConversation(npc, conversationId);
        return true;
    }
    
    protected virtual async void TalkToSelf(string conversationId)
    {
        Body.Velocity = Vector2.Zero;
        _camera.SetTemporaryTarget(this, new Vector2(0,-4), 4, null, TimeSpan.FromDays(10));
        
        SteeringStateMachine.SetSteeringState("Talking");
        
        await _narrationSystem.StartNarration(conversationId);

        var tcs2 = new TaskCompletionSource();
        _actionScheduler.Schedule(() =>
        {
            _camera.RemoveTemporaryTarget();
            tcs2.SetResult();
        });
        
        await tcs2.Task;
        await Task.Delay(200);
        
        _actionScheduler.Schedule(() =>
        { 
            SteeringStateMachine.SetSteeringState("Idle");
            OnGameStateUpdated();
        });
    }
    
    protected virtual void OnGameStateUpdated()
    {
    }

    protected virtual async void ExecuteStoryConversation(INpcCharacter npc, string conversationId = null)
    {
        if (!SteeringParameters.IsOnGround)
            return;
        
        Body.Velocity = Vector2.Zero;
        npc.PrepareCameraForTalking();

        var faceLeft = Body.Position.X > npc.Body.Position.X;
        
        if (conversationId is null)
        {
            var shouldTalk = true;
            var changedDir = false;

            var dist = npc.TalkingDistance;
            float targetPosX;
            
            while (true)
            {
                targetPosX = npc.Body.Position.X + (faceLeft ? dist : -dist);

                var aabb = Body.Colliders[0].Aabb
                    .Union(Body.Colliders[0].GetAabb(Body.Position with { X = targetPosX }));
                aabb.Inflate(0.1f, -0.1f);

                if (Services.Simulation.CheckCollision(aabb, Body))
                {
                    if (changedDir)
                    {
                        shouldTalk = false;
                        break;
                    }

                    changedDir = true;
                    faceLeft = !faceLeft;
                    continue;
                }

                break;
            }

            if (!shouldTalk)
            {
                return;
            }

            SteeringStateMachine.SetSteeringState("WalkTo");
            await WalkTo(targetPosX);
        }
        
        _actionScheduler.Schedule(() =>
        {
            Body.Velocity = Vector2.Zero;
            SteeringStateMachine.SetSteeringState("Talking");
            FaceLeft = faceLeft;
        });
        
        await npc.StartConversation(Body.Position.X, conversationId);

        var tcs = new TaskCompletionSource();
        _camera.SetTemporaryTarget(this, new Vector2(0, -2f), 5, () =>
        {
            tcs.SetResult();
        }, TimeSpan.Zero);

        await Task.Delay(50);
        await Task.WhenAny(tcs.Task, Task.Delay(200));
        
        _actionScheduler.Schedule(() =>
        {
            SteeringStateMachine.SetSteeringState("Idle");
        });
    }
    
    public async Task WalkTo(float targetPosX)
    {
        SteeringStateMachine.SetSteeringState("WalkTo");

        _walkToPositionX = targetPosX;
        
        var dir = MathF.Sign(targetPosX - Body.Position.X);
        FaceLeft = dir < 0;

        _walkToTaskCompletionSource = new TaskCompletionSource();
        await _walkToTaskCompletionSource.Task;
        _walkToTaskCompletionSource = null;
    }

    protected override void OnFixedUpdate(ref bool cancelUpdate)
    {
        base.OnFixedUpdate(ref cancelUpdate);
        
        if (_walkToPositionX.HasValue)
        {
            var dt = Services.Simulation.SimulationParameters.TimeDelta;
            
            var offset = MathF.Min(PhysicsValues.WalkSpeed * dt, MathF.Abs(_walkToPositionX.Value - Body.Position.X));;
            var dir = MathF.Sign(_walkToPositionX.Value - Body.Position.X);
            
            Body.KinematicMove(new Vector2(dir * offset, 0), KinematicMoveMode.Kinematic);
            
            if (MathF.Abs(Body.Position.X - _walkToPositionX.Value) < 0.025f)
            {
                if (!_walkToTaskCompletionSource.Task.IsCompleted)
                {
                    _walkToTaskCompletionSource.SetResult();
                }

                _walkToPositionX = null;
            }
        }
    }

    protected override void OnDispose(bool disposing)
    {
        _gameState.StateUpdated -= OnGameStateUpdated;
        base.OnDispose(disposing);
    }
}