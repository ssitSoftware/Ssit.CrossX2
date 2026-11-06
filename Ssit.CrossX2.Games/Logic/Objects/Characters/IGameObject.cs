using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Games.Audio;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public interface IGameObject
{
    ICommonSoundContainer CommonSoundContainer { get; }
    ContextSoundContainer SoundContainer { get; }
    TParameters GetParameters<TParameters>() where TParameters : class;
}