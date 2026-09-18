using Ssit.CrossX2.IoC;

namespace Ssit.CrossX2.Audio;

public static class AudioExtensions
{
    public static IIoCContainerBuilder WithCommonSoundsContainer(this IIoCContainerBuilder builder)
    {
        return builder.WithSingleton<ICommonSoundContainer, CommonSoundContainer>();
    }
}