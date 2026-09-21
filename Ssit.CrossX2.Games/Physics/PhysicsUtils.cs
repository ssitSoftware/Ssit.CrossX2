namespace Ssit.CrossX2.Framework.Games.Physics;

public static class PhysicsUtils
{
    public static void ApplyHorizontalFriction(ICollider collider, IMaterial material, float dt)
    {
        if(collider.AttachedBody == null) return;
        
        var fr1 = collider.Material?.Friction ?? 1;
        var fr2 = material?.Friction ?? 1;
        
        var newVelocity =  collider.AttachedBody.Velocity.X * (1 - MathF.Min(1, fr1 * fr2 * dt));
        collider.AttachedBody.Velocity = collider.AttachedBody.Velocity with { X = newVelocity };
    }

    public static IMaterial CloneAndUpdate(this IMaterial material, Action<IMaterial> updateAction)
    {
        var newMaterial = material.Clone();
        updateAction(newMaterial);
        return newMaterial;
    }
}