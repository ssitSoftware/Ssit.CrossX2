using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Services;

internal class FullHandlerMapper(IIoCContainer iocContainer) : HandlerMapper
{
    public void AddMappings(HandlerMapper mapper)
    {
        foreach(var pair in mapper.Mappings)
        {
            AddMapping(pair.Key, pair.Value);
        }
    }
    
    public override ViewHandler Create(View view, IViewParent parent)
    {
        if (!iocContainer.TryGet(typeof(IUiServices), out var uiServices))
        {
            throw new InvalidOperationException();
        }
        
        var page = parent.GetParent<IPage>();
        page.Styles.ApplyStyles(view, view.Style);
        
        ((IHandlerView)view).Initialize(uiServices as IUiServices);
        
        var type = view.CustomHandlerType ?? GetMapping(view.GetType());
        var handler = (ViewHandler)iocContainer.IoCConstruct(type, new ViewHandler.CreateHandlerParameters
        {
            View = view,
            Parent = parent,
            AdditionalParameters = view.CustomHandlerParameters
        });
        
        handler.Init();
        
        return handler;
    }
}