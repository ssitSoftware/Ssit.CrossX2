using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Transitions;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public abstract class ViewHandler: IDisposable
{
    public class CreateHandlerParameters
    {
        public View View;
        public IViewParent Parent;
        public object AdditionalParameters;
    }
    
    public View View { get; }
    public Type ViewType { get; }

    public IViewParent Parent { get; }

    protected RectangleF Bounds { get; private set; }

    public RectangleF ScreenBounds { get; private set; }

    protected float CurrentScale => Parent.GetParent<IPage>().Scale;

    // Last known value of View.Visible - a composed SharedBool raises ValueChanged even when its value stays.
    private bool _visible;

    // Show / Hide transition started by a change of View.Visible: its type (None = no transition runs) and progress
    // in the page transition convention - 0 = fully shown, 1 = fully hidden (Show goes 1 -> 0, Hide 0 -> 1).
    private TransitionType _visibilityTransitionType;
    private float _visibilityTransition;
    
    protected ViewHandler(Type type, CreateHandlerParameters parameters)
    {
        View = parameters.View;
        Parent = parameters.Parent;
        ViewType = type;

        if (((IHandlerView)View).Handler != null) throw new InvalidOperationException();
        View.Handler = this;

        // Already visible when created - no Show transition.
        _visible = View.Visible.Value;
        View.Visible.ValueChanged += OnVisibleChanged;
    }

    /// <summary>
    /// Showing / hiding a view starts its Show / Hide transition and changes the layout of its parents (hidden views
    /// take no space in stacks).
    /// </summary>
    private void OnVisibleChanged()
    {
        var visible = View.Visible.Value;

        if (visible != _visible)
        {
            _visible = visible;
            StartVisibilityTransition(visible);
        }

        Parent?.GetParent<IPage>(true)?.RecalculateLayout();
    }

    /// <summary>
    /// Starts the Show (<paramref name="visible"/>) or Hide transition - or shows / hides the view at once when it has
    /// no transition of that type. A transition reversed half way continues from where the running one is.
    /// </summary>
    private void StartVisibilityTransition(bool visible)
    {
        var type = visible ? TransitionType.Show : TransitionType.Hide;

        if (View.VisibilityTransitionTime <= 0 || !HasTransition(type))
        {
            _visibilityTransitionType = TransitionType.None;
            _visibilityTransition = 0;
            return;
        }

        if (_visibilityTransitionType == TransitionType.None)
        {
            _visibilityTransition = visible ? 1 : 0;
        }

        _visibilityTransitionType = type;
    }

    private bool HasTransition(TransitionType type)
    {
        if (View.Transitions is null)
            return false;

        foreach (var transition in View.Transitions)
        {
            if ((transition.ForTransitions & type) != 0)
                return true;
        }

        return false;
    }

    private void UpdateVisibilityTransition(float dt)
    {
        if (_visibilityTransitionType == TransitionType.None)
            return;

        var step = dt / View.VisibilityTransitionTime;

        if (_visibilityTransitionType == TransitionType.Show)
        {
            _visibilityTransition -= step;

            if (_visibilityTransition > 0)
                return;
        }
        else
        {
            _visibilityTransition += step;

            if (_visibilityTransition < 1)
                return;
        }

        // Finished - fully shown, or hidden (not drawn any more).
        _visibilityTransitionType = TransitionType.None;
        _visibilityTransition = 0;
    }

    protected Length CalculateLengthWithPadding(Length length, Length? pad1, Length? pad2)
    {
        var p1 = pad1 ?? Length.Zero;
        var p2 = pad2 ?? Length.Zero;

        if (p1.IsAuto)
        {
            p1 = Length.Zero;
        }
        
        if (p2.IsAuto)
        {
            p2 = Length.Zero;
        }

        var add = Length.Zero;
        add += p1.Value + p2.Value;

        var percent = p1.Percent + p2.Percent;

        var rest = 1 - percent;
        if (rest <= 0)
        {
            throw new InvalidProgramException("Percent value of paddings cannot 100% or more!");
        }

        var restSize = (length + add).Calculate(CurrentScale, 0);
        var totalSize = MathF.Ceiling(restSize / rest);

        return new Length(pixels: totalSize);
    }
    
    ~ViewHandler()
    {
        OnDispose(false);
    }

    public virtual void Init()
    {
    }

    public virtual void SetBounds(RectangleF rectangleF)
    {
        Bounds = rectangleF;
        UpdateScreenBounds();
    }

    private void UpdateScreenBounds()
    {
        var offset = Parent?.ScreenBounds.TopLeft ?? Vector2.Zero;
        ScreenBounds = new RectangleF(offset + Bounds.TopLeft, Bounds.Size);
    }

    public virtual void Draw(IRenderer renderer)
    {
        // A hidden view is still drawn while its Hide transition runs.
        if (View.Visible.Value || _visibilityTransitionType == TransitionType.Hide)
        {
            var applyTransitions = View.Transitions?.Length > 0;
            
            if (applyTransitions)
            {
                var page = Parent?.GetParent<IPage>();
                applyTransitions = !IsSharedWithOtherPage(page);

                if (applyTransitions)
                {
                    for (var idx = 0; idx < View.Transitions.Length; ++idx)
                    {
                        View.Transitions[idx].Apply(View.Handler, renderer, CurrentScale, page?.TransitionType ?? 0, page?.TransitionProgress ?? 0);
                    }
                }
            }
            
            var applyVisibilityTransitions = _visibilityTransitionType != TransitionType.None;

            if (applyVisibilityTransitions)
            {
                for (var idx = 0; idx < View.Transitions.Length; ++idx)
                {
                    View.Transitions[idx].Apply(View.Handler, renderer, CurrentScale, _visibilityTransitionType, _visibilityTransition);
                }
            }

            OnDraw(renderer);

            if (applyVisibilityTransitions)
            {
                for (var idx = View.Transitions.Length - 1; idx >= 0; --idx)
                {
                    View.Transitions[idx].Finish(renderer);
                }
            }
            
            if (applyTransitions)
            {
                for (var idx = View.Transitions.Length - 1; idx >=0 ; --idx)
                {
                    View.Transitions[idx].Finish(renderer);
                }
            }
        }
    }

    /// <summary>
    /// True when the other page of the current navigation contains a view with the same shared instance name.
    /// </summary>
    private bool IsSharedWithOtherPage(IPage page)
    {
        return !string.IsNullOrEmpty(View.SharedInstance) &&
               page?.SharedInstancesInTransition.Contains(View.SharedInstance) == true;
    }

    /// <summary>
    /// Updates the view. Overrides must call the base (it runs the Show / Hide transition).
    /// </summary>
    public virtual void Update(float dt)
    {
        UpdateVisibilityTransition(dt);
    }

    protected virtual void OnDraw(IRenderer renderer)
    {
        
    }
    
    public void Dispose()
    {
        View.Visible.ValueChanged -= OnVisibleChanged;
        OnDispose(true);
        View.Handler = null;
    }

    protected virtual void OnDispose(bool disposing)
    {
    }

    protected virtual void OnRecalculatePositionAndSize()
    {
        Parent?.RecalculateLayout(View);
    }

    internal void RecalculatePositionAndSize()
    {
        OnRecalculatePositionAndSize();
    }

    public virtual void CalculateSize(out Length width, out Length height)
    {
        width = View.Width ?? Length.Fill;
        height = View.Height ?? Length.Fill;
    }

    public virtual void CalculateAlign(out Align horizontalAlign, out Align verticalAlign)
    {
        horizontalAlign = View.HorizontalAlign ?? Align.Fill;
        verticalAlign = View.VerticalAlign ?? Align.Fill;
    }

    protected void SignalRecalculationPending()
    {
        var page = Parent?.GetParent<IPage>();
        if (page is not null)
        {
            page.SignalRecalculationPending();
        }
    }
}

public abstract class ViewHandler<TView>(ViewHandler.CreateHandlerParameters parameters)
    : ViewHandler(typeof(TView), parameters)
    where TView : View
{
    protected TView AttachedView => (TView)View;
}