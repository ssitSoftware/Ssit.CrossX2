using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Drops through a one-way platform (top-only collision) when Jump is pressed while holding Down.
/// The character is moved dropDistance down to get below the platform and enters fallState.
/// </summary>
public class DropThroughPlatformBehavior(float dropDistance = 0.25f, string fallState = "Fall") : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringInput.Button(SteeringControlNames.Jump) != ButtonState.JustPressed)
            return false;

        if (obj.SteeringInput.Value(SteeringControlNames.VerticalMove) <= 0.75f)
            return false;

        // Only top-sided (one-way) platforms can be dropped through; on solid ground the press is just consumed
        var groundMaterial = obj.SteeringParameters.GroundMaterial;
        if (groundMaterial is null || groundMaterial.Sides != ColliderSides.Top)
            return true;

        obj.Body.Position += new Vector2(0, dropDistance);
        obj.SetSteeringState(fallState);
        return true;
    }
}
