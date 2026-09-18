using Ssit.CrossX2.IoC;
using Ssit.CrossX2.UI.Common.Services;

namespace Ssit.CrossX2.UI.Common;

public static class CommonExtensions
{
    public static IIoCContainerBuilder WithTranslator(this IIoCContainerBuilder builder, string languagesPath)
    {
        return builder
            .WithSingleton<ITranslator, Translator>(languagesPath);
    }
    
    public static IIoCContainerBuilder WithTranslator(this IIoCContainerBuilder builder, params string[] languagesPaths)
    {
        return builder
            .WithSingleton<ITranslator, Translator>(languagesPaths);
    }
}