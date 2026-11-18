using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

/// <summary>
/// Per-bot data of <see cref="InvestigateBehavior"/>.
/// </summary>
public class InvestigateParameters
{
    /// <summary>X position the investigation started at.</summary>
    public float StartX;

    /// <summary>Direction to run in.</summary>
    public bool FaceLeft;

    /// <summary>Time since the investigation started.</summary>
    public float Time;

    /// <summary>Farthest distance run so far in <see cref="FaceLeft"/> direction.</summary>
    public float BestTravelled;

    /// <summary>Time without getting any farther (blocked by a wall too tall to jump over).</summary>
    public float StuckTime;

    /// <summary>
    /// (Re)starts the investigation from the given position in the given direction - call before switching the brain
    /// to the state with <see cref="InvestigateBehavior"/> (also when it is already in it, e.g. hit again).
    /// </summary>
    public void Start(float x, bool faceLeft)
    {
        StartX = x;
        FaceLeft = faceLeft;
        Time = 0;
        BestTravelled = 0;
        StuckTime = 0;
    }
}

/// <summary>
/// Brain behavior: runs in the direction set by <see cref="InvestigateParameters.Start"/> (e.g. towards where an
/// unseen attacker hit from) for <paramref name="distance"/> tiles, then switches to <paramref name="doneStateName"/>.
/// Also ends when it doesn't get any farther for <paramref name="stuckTime"/> seconds or after <paramref name="maxTime"/>.
/// Put scan behaviors before it (seeing the target starts a chase) and <see cref="JumpOverObstacleBehavior"/> after it.
/// </summary>
public class InvestigateBehavior(float distance, string doneStateName, float stuckTime = 1.5f, float maxTime = 6f) : Behavior<IBrainCharacter>
{
    /// <summary>Getting farther by less than this does not count as progress.</summary>
    private const float MinProgress = 0.05f;

    protected override void OnLeaveState(IBrainCharacter obj)
    {
        base.OnLeaveState(obj);
        // Release movement input so it does not leak into the next state
        obj.InputController.ResetManualInput();
    }

    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<InvestigateParameters>();
        var x = obj.Body.Position.X;
        parameters.Time += dt;

        var travelled = parameters.FaceLeft ? parameters.StartX - x : x - parameters.StartX;

        // The stuck time also covers the hit reaction and knockback at the start (it pushes backwards)
        if (travelled > parameters.BestTravelled + MinProgress)
        {
            parameters.BestTravelled = travelled;
            parameters.StuckTime = 0;
        }
        else
        {
            parameters.StuckTime += dt;
        }

        if (travelled >= distance || parameters.StuckTime >= stuckTime || parameters.Time >= maxTime)
        {
            obj.SetBrainState(doneStateName);
            return true;
        }

        // Run (not walk) towards where the hit came from
        obj.FaceLeft = parameters.FaceLeft;
        obj.InputController.SetValue(SteeringControlNames.HorizontalMove, parameters.FaceLeft ? -1f : 1f);
        obj.InputController.SetButtonState(SteeringControlNames.Walk, ButtonState.Empty);
        return false;
    }
}
