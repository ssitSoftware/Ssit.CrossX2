using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Handlers;

public interface IUiCommandHandler
{
    bool OnUiButton(UiButton button, IInputContext context);
}