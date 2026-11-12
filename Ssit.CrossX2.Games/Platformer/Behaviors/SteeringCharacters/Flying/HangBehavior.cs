using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>
/// Keeps the object hanging in place (e.g. a bat under a ceiling): the body is kinematic while in the state,
/// so it neither falls nor gets pushed. Leaving the state makes the body dynamic again.
/// </summary>
public class HangBehavior : SingleSteeringBehavior<ISteeringCharacter, HangBehavior>
{
    protected override void OnEnter(ISteeringCharacter character)
    {
        character.Body.Velocity = Vector2.Zero;
        character.Body.IsKinematic = true;
    }

    protected override void OnExit(ISteeringCharacter obj)
    {
        obj.Body.IsKinematic = false;
        // Wake the body up so physics (gravity) applies again immediately
        obj.Body.Touch();
    }
}
