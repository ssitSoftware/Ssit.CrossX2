using System.Diagnostics.CodeAnalysis;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Services;

public interface IHandlerMapper
{
    IHandlerMapper AddMapping<TView, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewHandler>()
        where TView : View where TViewHandler : ViewHandler<TView>;

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    Type GetMapping(Type viewType);

    ViewHandler Create(View view, IViewParent parent);
}