using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public class GameObjectsServices(
    ISimulation simulation,
    IContentManager contentManager,
    IIoCContainer container,
    IGameTemplate gameTemplate,
    ICommonSoundContainer commonSoundContainer,
    IParticleSystem particleSystem)
{
    public ISimulation Simulation { get; } = simulation;
    public IContentManager ContentManager { get; } = contentManager;
    public IIoCContainer Container { get; } = container;
    public IGameTemplate GameTemplate { get; } = gameTemplate;
    public ICommonSoundContainer CommonSoundContainer { get; } = commonSoundContainer;
    public IParticleSystem ParticleSystem { get; } = particleSystem;
}