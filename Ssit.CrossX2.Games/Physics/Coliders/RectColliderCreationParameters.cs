using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Physics.Coliders;

public class RectColliderCreationParameters : ColliderCreationParameters
{
    public Vector2 Center { get; set; } = Vector2.Zero;
    public SizeF Size { get; set; }
}