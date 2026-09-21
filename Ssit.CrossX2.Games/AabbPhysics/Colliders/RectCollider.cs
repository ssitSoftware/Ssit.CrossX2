using System.Numerics;
using Ssit.CrossX2.Framework.Games.AabbPhysics.Algorithms;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;

namespace Ssit.CrossX2.Framework.Games.AabbPhysics.Colliders;

internal class RectCollider : ICollider
{
    public IBody AttachedBody { get; }

    public ColliderType Type { get; }

    public IMaterial Material { get; }

    public Aabb Aabb => GetAabb(AttachedBody?.Position ?? Vector2.Zero);

    public string Name { get; }
    public bool IsActive { get; set; }

    public Aabb GetAabb(Vector2 position)
    {
        return new Aabb
        {
            Left = position.X + _center.X - _halfSize.Width,
            Right = position.X + _center.X + _halfSize.Width,
            Top = position.Y + _center.Y - _halfSize.Height,
            Bottom = position.Y + _center.Y + _halfSize.Height,
        };
    }

    private readonly Vector2 _center;
    private readonly SizeF _halfSize;

    public event CollisionDelegate CollisionWith;

    public void RaiseCollisionWith(bool byMyMovement, ICollider other, Vector2 impact)
    {
        CollisionWith?.Invoke(byMyMovement, other, impact);
        ((Body)AttachedBody)?.PostOnColision(this, other, impact);
    }

    public RectCollider(RectColliderCreationParameters creationParameters)
    {
        AttachedBody = creationParameters.AttachToBody;
        Type = creationParameters.Type;
        Material = creationParameters.Material;
        _center = creationParameters.Center;
        _halfSize = creationParameters.Size / 2;
        Name = creationParameters.Name;
        IsActive = creationParameters.Active;
    }

    public bool CheckCollisionWith(ICollider obstacle)
    {
        if(obstacle is RectCollider rectCollider)
        {
            return RectRectCollision.Check(this, rectCollider);
        }

        throw new NotSupportedException();
    }

    public bool GetMovementCollision(ICollider obstacle, ref Vector2 move, out Vector2 normal)
    {
        if (obstacle is RectCollider rectCollider)
        {
            return RectRectCollision.GetMovementCollision(this, rectCollider, ref move, out normal);
        }

        throw new NotSupportedException();
    }

    public void Serialize(BinaryWriter binaryWriter)
    {
        binaryWriter.Write(_center.X);
        binaryWriter.Write(_center.Y);
        
        binaryWriter.Write(_halfSize.Width);
        binaryWriter.Write(_halfSize.Height);
        
        binaryWriter.Write(Material.Index);
        binaryWriter.Write((int)Type);
        binaryWriter.Write(IsActive);
    }

    public static RectCollider Deserialize(BinaryReader reader, IMaterial[] materials)
    {
        var centerX = reader.ReadSingle();
        var centerY = reader.ReadSingle();
        var width = reader.ReadSingle() * 2;
        var height = reader.ReadSingle() * 2;

        var materialIndex = reader.ReadInt32();
        var type = (ColliderType)reader.ReadInt32();
        var active = reader.ReadBoolean();
        
        
        return new RectCollider(new RectColliderCreationParameters
        {
            Center = new Vector2(centerX, centerY),
            Size = new SizeF(width, height),
            Material = materials[materialIndex],
            Type = type,
            Active = active,
        });
    }
}