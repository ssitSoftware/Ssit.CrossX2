using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Ssit.CrossX2.UI.Services;

internal class NavigationMap : INavigationMap
{
    private readonly Dictionary<Type, Type> _vmToPageMappings = new();

    public bool HasAny => _vmToPageMappings.Count > 0;
    
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type GetPageTypeFromViewModel(object viewModel)
    {
        var type = viewModel.GetType();
        if (!_vmToPageMappings.TryGetValue(type, out var result))
        {
            throw new InvalidOperationException();
        }

        // The dictionary is only populated via Map<TViewModel, TPage>() where TPage is annotated
        // with [DynamicallyAccessedMembers(PublicConstructors)], so constructors are preserved.
        return result;
    }

    public INavigationMap Map<TViewModel, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPage>()
        where TViewModel : class where TPage : Page<TViewModel>
    {
        _vmToPageMappings[typeof(TViewModel)] = typeof(TPage);
        return this;
    }

    public INavigationMap AutoScan(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAssignableTo(typeof(IPage)) && !type.IsAbstract)
            {
                var args = type.BaseType?.GenericTypeArguments ?? [];
                if (args.Length == 1)
                {
                    _vmToPageMappings[args[0]] = type; 
                }
            }
        }
        return this;
    }
}