using System.Diagnostics.CodeAnalysis;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Services;

internal class HandlerMapper : IHandlerMapper
{
    public IReadOnlyDictionary<Type, Type> Mappings => _mappings;
    private readonly Dictionary<Type, Type> _mappings = new();

    public IHandlerMapper AddMapping<TView, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewHandler>()
        where TView : View where TViewHandler : ViewHandler<TView>
    {
        _mappings[typeof(TView)] = typeof(TViewHandler);
        return this;
    }

    protected void AddMapping(Type viewType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type handlerType)
    {
        _mappings[viewType] = handlerType;
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type GetMapping(Type viewType)
    {
        var type = viewType;

        while (type != null && type != typeof(View))
        {
            if (_mappings.TryGetValue(viewType, out Type handlerType))
            {
                // The dictionary is only populated via AddMapping<TView, TViewHandler>() where TViewHandler
                // is annotated with [DynamicallyAccessedMembers(PublicConstructors)], so constructors are preserved.
                return handlerType;
            }
            type = type.BaseType;
        }

        throw new KeyNotFoundException($"No handler registered for {viewType}");
    }

    public virtual ViewHandler Create(View view, IViewParent parent)
    {
        throw new NotSupportedException();
    }
}