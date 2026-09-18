using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Common.Services;

public interface ITranslator
{
    event Action LanguageChanged;
    SharedString this[string key] { get; }
    void ToggleLanguage(bool previous = false);
    int CurrentLanguage { get; set; }
}