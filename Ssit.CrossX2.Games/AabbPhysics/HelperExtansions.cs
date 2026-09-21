using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.AabbPhysics;

public static class HelperExtansions
{
    public static Vector2 VelocitySum(this IBody body) => body.Velocity + body.KinematicVelocity;
}