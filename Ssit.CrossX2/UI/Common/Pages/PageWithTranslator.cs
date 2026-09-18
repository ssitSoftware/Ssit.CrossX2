using Ssit.CrossX2.UI.Common.Services;

namespace Ssit.CrossX2.UI.Common.Pages;

public abstract class PageWithTranslator<TViewModel> : Page<TViewModel> where TViewModel : class
{
    protected ITranslator Translator => field ??= Services.Get<ITranslator>();
}