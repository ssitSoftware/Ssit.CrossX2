using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Common.Services;

public interface ITranslator
{
    event Action LanguageChanged;
    SharedString this[string key] { get; }
    void ToggleLanguage(bool previous = false);
    int CurrentLanguage { get; set; }
}