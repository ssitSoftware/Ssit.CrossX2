using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Logic;

public static class PhysicsHelpers
{
    private static readonly List<ICollider> QueryList = new();

    public static Func<int, bool> IsOnPlatform;
    
    public static void DetectOnGround(this SpriteGameObject spriteGameObject, out bool isOnGround, out bool isOnPlatform, out bool isOnStaticGround, out int groundMaterial)
    {
        isOnGround = false;
        isOnPlatform = true;
        isOnStaticGround = true;
        groundMaterial = 0;

        var group = spriteGameObject.Body.Colliders[0].Material.ColliderGroup;
        
        var leftX = spriteGameObject.FaceLeft ? 0.2f : 0.3f;
        var rightX = spriteGameObject.FaceLeft ? 0.3f : 0.2f;
        
        var aabb = new Aabb(spriteGameObject.Body.Position - new Vector2(leftX, 0.05f), spriteGameObject.Body.Position + new Vector2(rightX, 0.25f));
        spriteGameObject.Services.Simulation.CheckCollision(aabb, spriteGameObject.Body, group,0, QueryList);

        aabb = spriteGameObject.Body.Colliders[0].Aabb;
        
        foreach (var collider in QueryList)
        {
            if (collider.Aabb.Top < aabb.Bottom - spriteGameObject.Body.Simulation.MovementEpsilon) continue;
            
            isOnGround = true;

            isOnPlatform &= IsOnPlatform?.Invoke(collider.Material.Index) ?? false;

            if (collider.Type != ColliderType.Static)
            {
                isOnStaticGround = false;
            }

            groundMaterial = Math.Max(collider.Material.Index, groundMaterial);
        }
        
        isOnStaticGround &= isOnGround;
        isOnPlatform &= isOnGround;
        
        QueryList.Clear();
    }
    
    public static void DetectOnGround(this SpriteGameObject2 spriteGameObject, out bool isOnGround, out bool isOnPlatform, out bool isOnStaticGround, out IMaterial groundMaterial, Vector2 epsilon)
    {
        isOnGround = false;
        isOnPlatform = true;
        isOnStaticGround = true;
        groundMaterial = null;

        var aabb = spriteGameObject.Body.Colliders[0].GetAabb(Vector2.Zero);
        var group = spriteGameObject.Body.Colliders[0].Material.ColliderGroup;
        
        var leftX = aabb.Width / 2 - 0.001f + epsilon.X;
        var rightX = aabb.Width / 2 - 0.001f + epsilon.X;
        
        aabb = new Aabb(spriteGameObject.Body.Position - new Vector2(leftX, 0.001f), spriteGameObject.Body.Position + new Vector2(rightX, 0.1f + epsilon.Y));
        spriteGameObject.Services.Simulation.CheckCollision(aabb, spriteGameObject.Body, group, 0, QueryList);
        
        aabb = spriteGameObject.Body.Colliders[0].Aabb;
        
        foreach (var collider in QueryList)
        {
            if (collider.Aabb.Top < aabb.Bottom - spriteGameObject.Body.Simulation.MovementEpsilon) continue;
            
            isOnGround = true;
            isOnPlatform &= IsOnPlatform?.Invoke(collider.Material.Index) ?? false;
            
            if (collider.Type != ColliderType.Static)
            {
                isOnStaticGround = false;
            }

            if ((groundMaterial?.Index ?? -1) < collider.Material.Index)
            {
                groundMaterial = collider.Material;
            }
        }
        
        isOnStaticGround &= isOnGround;
        isOnPlatform &= isOnGround;
        
        QueryList.Clear();
    }
}