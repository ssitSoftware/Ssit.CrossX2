using Ssit.CrossX2.Framework.Games.AabbPhysics.Colliders;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;

namespace Ssit.CrossX2.Framework.Games.AabbPhysics;

internal static class CollidersFactory
{
    public static ICollider Create<TCreationParameters>(TCreationParameters creationParameters)
    {
        switch(creationParameters)
        {
            case RectColliderCreationParameters rectColliderCreationParameters:
                return new RectCollider(rectColliderCreationParameters);
        }
        throw new NotSupportedException();
    }
}