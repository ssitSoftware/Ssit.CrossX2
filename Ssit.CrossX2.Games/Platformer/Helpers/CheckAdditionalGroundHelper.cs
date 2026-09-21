using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Helpers;

public class CheckAdditionalGroundHelper(params int[] excludeMaterials)
{
    private readonly HashSet<int> _excludesMaterials = [..excludeMaterials];
    
    public bool IsOnGroundExtra(ISteeringCharacter obj)
    {
        if (!obj.SteeringParameters.IsOnGround)
        {
            var aabb =  obj.Body.Colliders[0].Aabb;

            if (obj.FaceLeft)
            {
                aabb.Right += 0.5f;
                aabb.Left += 0.1f;
            }
            else
            {
                aabb.Left -= 0.3f;
                aabb.Right -= 0.1f;
            }

            aabb.Top = aabb.Bottom - 0.05f;
            aabb.Bottom += 0.4f;
            
            var group = obj.Body.Colliders[0].Material.ColliderGroup;
            
            var colliders = obj.Body.Simulation.GetColliders(aabb, obj.Body, group);
            if (colliders.Count == 0)
                return false;

            foreach (var collider in colliders)
            {
                if (_excludesMaterials.Contains(collider.Material.Index))
                {
                    return false;
                }
            }
        }

        return true;
    }
}