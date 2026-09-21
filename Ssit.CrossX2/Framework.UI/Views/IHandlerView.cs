using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Services;

namespace Ssit.CrossX2.Framework.UI.Views;

internal interface IHandlerView
{
    ViewHandler Handler { get; }
    void Initialize(IUiServices services);
}