using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Transitions;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

public abstract class View: IHandlerView
{
    public Guid Guid { get; } = Guid.NewGuid();
    
    public Align? HorizontalAlign { get; set; }
    public Align? VerticalAlign { get; set; }
    
    public Length? AnchorX { get;set; }
    public Length? AnchorY { get;set; }
    
    public Length? Width { get; set; }
    public Length? Height { get; set; }

    public SharedBool Visible { get; set; } = true;

    public ITransition[] Transitions { get; set; }
    
    public string Style { get; set; } = "";
    
    ViewHandler IHandlerView.Handler => Handler;

    public Type CustomHandlerType
    {
        set
        {
            if (!typeof(ViewHandler).IsAssignableFrom(value))
            {
                throw new ArgumentException($"Type '{value}' is not a view handler");
            }

            var type = value;

            while (type != null && type != typeof(ViewHandler) && type != typeof(object))
            {
                if (typeof(ViewHandler<>).MakeGenericType(GetType()).IsAssignableFrom(type))
                {
                    field = type;
                    return;
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Type '{value}' is not a view handler");
        }

        internal get;
    }

    public object CustomHandlerParameters { get; set; }
    
    public void RecalculatePositionAndSize() => Handler?.RecalculatePositionAndSize();

    internal ViewHandler Handler { get; set; }

    void IHandlerView.Initialize(IUiServices services) => Initialize(services);
    
    protected virtual void Initialize(IUiServices services)
    {
    }
}