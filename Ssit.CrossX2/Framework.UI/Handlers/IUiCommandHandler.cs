using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public interface IUiCommandHandler
{
    bool OnUiButton(UiButton button, IInputContext context);
}