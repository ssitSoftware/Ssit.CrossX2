using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Attributes;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;

namespace Ssit.CrossX2.Framework.Games.AabbPhysics;

[SupportedColliders(typeof(RectColliderCreationParameters))]
public class SimulationFactory: ISimulationFactory
{
    public ISimulation CreateSimulation() => new Simulation();
}