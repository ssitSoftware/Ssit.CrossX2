using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class WallContactCollisionBehavior(WallContactCollisionBehavior.DefaultMode mode, int wallClimbMaterialIndex, params string[] wallSlideFromStates): SteeringBehavior<ISteeringCharacter>
{
    public enum DefaultMode
    {
        SwitchDirection,
        GoIdle,
        PushToSlide,
        CheckWallSlideButton
    }
    
    private readonly HashSet<string> _wallSlideFromStates = new(wallSlideFromStates);

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
        if (MathF.Abs(impact.X) > 0.01f)
        {
            if (other.Aabb.Bottom > source.Aabb.Center.Y && !_noSlideMaterials.Contains(other.Material.Index))
            {
                if ((!obj.SteeringParameters.IsOnGround || mode == DefaultMode.SwitchDirection) && other.Material.Index == wallClimbMaterialIndex)
                {
                    obj.SetSteeringState("WallClimb");
                    return true;
                }

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