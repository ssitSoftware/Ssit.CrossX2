using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Physics;

public class SimulationParameters
{
    public float TimeDelta { get; set; }
    public float MaxHorizontalSpeed { get; set; }
    public float MaxVerticalSpeed { get; set; }
    public Vector2 GravityAcceleration { get; set; }
}