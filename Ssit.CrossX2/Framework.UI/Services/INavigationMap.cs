using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Ssit.CrossX2.Framework.UI.Services;

public interface INavigationMap
{
    INavigationMap Map<TViewModel, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPage>()
        where TViewModel : class where TPage : Page<TViewModel>;

    INavigationMap AutoScan(Assembly assembly);
}