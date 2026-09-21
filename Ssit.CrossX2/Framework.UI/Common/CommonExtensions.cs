using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.UI.Common.Services;

namespace Ssit.CrossX2.Framework.UI.Common;

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