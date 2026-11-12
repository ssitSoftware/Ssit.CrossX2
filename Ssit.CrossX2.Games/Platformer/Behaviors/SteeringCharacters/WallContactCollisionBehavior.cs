using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Reacts to the character bumping into a wall horizontally. Depending on the situation it starts a wall climb, a wall slide,
/// or (on the ground) turns around / goes idle. wallSlideFromStates are the states from which a slide may begin.
/// </summary>
public class WallContactCollisionBehavior(WallContactCollisionBehavior.DefaultMode mode, int wallClimbMaterialIndex, params string[] wallSlideFromStates): SteeringBehavior<ISteeringCharacter>
{
    /// <summary>
    /// What the character does when it hits a wall.
    /// </summary>
    public enum DefaultMode
    {
        // Turn around (typical for patrolling enemies); also allows wall climbing from the ground
        SwitchDirection,
        // Stop and become idle
        GoIdle,
        // Slide when the player pushes strongly toward the wall
        PushToSlide,
        // Slide only while the WallSlide button is held
        CheckWallSlideButton
    }
    
    private readonly HashSet<string> _wallSlideFromStates = new(wallSlideFromStates);

    // Collider materials the character can neither slide down nor climb
    private readonly HashSet<int> _noSlideMaterials = new();
    
    public int[] NoSlideMaterials
    {
        set
        {
            foreach (var material in value)
            {
                _noSlideMaterials.Add(material);
            }
        }
    }

    protected override bool OnCollision(ISteeringCharacter obj, ICollider source, ICollider other, Vector2 impact)
    {
        // Only react to horizontal impacts
        if (MathF.Abs(impact.X) > 0.01f)
        {
            // The wall must reach below the character's center and be of a slidable material
            if (other.Aabb.Bottom > source.Aabb.Center.Y && !_noSlideMaterials.Contains(other.Material.Index))
            {
                if ((!obj.SteeringParameters.IsOnGround || mode == DefaultMode.SwitchDirection) && other.Material.Index == wallClimbMaterialIndex)
                {
                    obj.SetSteeringState("WallClimb");
                    return true;
                }

                // Wall slide only starts in the air and only from the allowed states
                var stateName = obj.CurrentSteeringState.Name;
                if (!obj.SteeringParameters.IsOnGround && _wallSlideFromStates.Contains(stateName))
                {
                    if (mode == DefaultMode.CheckWallSlideButton)
                    {
                        if (obj.SteeringInput.Button(SteeringControlNames.WallSlide).IsDown)
                        {
                            obj.SetSteeringState("WallSlide");
                            return true;
                        }
                    }
                    else if (mode == DefaultMode.PushToSlide)
                    {
                        // Pushing toward the wall (opposite to the impact direction)
                        var move = obj.SteeringInput.Value(SteeringControlNames.HorizontalMove);
                        if (MathF.Abs(move) >= 0.75f && MathF.Sign(move) == MathF.Sign(-impact.X))
                        {
                            obj.SetSteeringState("WallSlide");
                            return true;
                        }
                    }
                    else
                    {
                        obj.SetSteeringState("WallSlide");
                        return true;
                    }
                }
            }

            var aabb = source.Aabb;
            // Ignore small steps: only a wall that overlaps the upper part of the body counts as blocking
            aabb.Bottom -= 0.5f;

            if (aabb.Intersects(other.Aabb) && obj.SteeringParameters.IsOnStaticGround)
            {
                if (mode == DefaultMode.SwitchDirection)
                {
                    obj.FaceLeft = !obj.FaceLeft;
                }
                else
                {
                    obj.SetSteeringState("Idle");
                }

                return true;
            }
        }

        return false;
    }
    
    
}