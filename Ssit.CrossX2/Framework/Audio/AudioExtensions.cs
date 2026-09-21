using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Audio;

public static class AudioExtensions
{
    public static IIoCContainerBuilder WithCommonSoundsContainer(this IIoCContainerBuilder builder)
    {
        return builder.WithSingleton<ICommonSoundContainer, CommonSoundContainer>();
    }
}