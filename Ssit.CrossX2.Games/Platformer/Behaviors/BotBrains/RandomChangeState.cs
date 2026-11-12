using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

/// <summary>
/// Every checkPeriod seconds switches to newStateName with the given probability (e.g. idle &lt;-&gt; wander variety).
/// </summary>
public class RandomChangeState(string newStateName, float probability, float checkPeriod): Behavior<IBrainCharacter>
{
    private class Parameters
    {
        // Time of the last roll
        public DateTime LastCheck = DateTime.Now;
    }
    
    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<Parameters>();
        if (DateTime.Now - parameters.LastCheck < TimeSpan.FromSeconds(checkPeriod))
        {
            return false;
        }

        parameters.LastCheck = DateTime.Now;

        if (MersenneTwister.Shared.NextSingle() < probability)
        {
            obj.SetBrainState(newStateName);
            // Release any input held by the previous state
            obj.InputController.ResetManualInput();
            return true;
        }
        
        return false;
    }
}