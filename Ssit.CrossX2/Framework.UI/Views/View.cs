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

    /// <summary>
    /// Transitions of the view: page transitions (navigation) and the Show / Hide transitions which run when
    /// <see cref="Visible"/> changes (see <see cref="VisibilityTransitionTime"/>).
    /// </summary>
    public ITransition[] Transitions { get; set; }

    /// <summary>
    /// Duration (seconds) of the Show / Hide transition started by a change of <see cref="Visible"/> - only when
    /// <see cref="Transitions"/> contain a transition for <see cref="TransitionType.Show"/> (resp.
    /// <see cref="TransitionType.Hide"/>). Without one the view shows / hides at once. A hidden view is drawn until
    /// its Hide transition ends. The initial visibility doesn't start a transition. Default: 0.5 s.
    /// </summary>
    public float VisibilityTransitionTime { get; set; } = 0.5f;

    /// <summary>
    /// Name of the instance shared between pages. When both pages of a navigation contain a view with the same
    /// shared instance name, the view doesn't use its transitions - it stays still while the pages are switched.
    /// </summary>
    public string SharedInstance { get; set; }
    
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