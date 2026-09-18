using Ssit.CrossX2.UI.Handlers;
using Ssit.CrossX2.UI.Services;

namespace Ssit.CrossX2.UI.Views;

internal interface IHandlerView
{
    ViewHandler Handler { get; }
    void Initialize(IUiServices services);
}