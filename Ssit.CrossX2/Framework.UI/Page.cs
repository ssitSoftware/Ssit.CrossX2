using System.Diagnostics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.UI.Exceptions;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Transitions;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI;

public abstract class Page<TViewModel>: View, IPage where TViewModel: class
{
    protected TViewModel ViewModel { get; private set; }

    object IPage.ViewModel => ViewModel;
    
    private StylesContainer _styles;

    private IIoCContainer _iocContainer;
    private ViewHandler _rootHandler;
    private RectangleF _screenBounds;
    private bool _recalculateLayout;
    private bool _recalculationNeeded;
    private bool _renderingInvalid;

    private FocusWalker _focusWalker;
    
    private RectangleF _bounds;
    private float _transitionProgress;

    private int _nextId = 1;
    
    protected IIoCContainer Services => _iocContainer;

    protected IFocusable FocusedElement => _focusWalker.FocusedElement;

    public virtual float TransitionTime => 0f;
    
    float IPage.TransitionProgress { get => _transitionProgress; set => _transitionProgress = value; }

    StylesContainer IPage.Styles => _styles;
    
    protected bool IsInTransition => _transitionProgress > 0;
    
    protected SizeF ScreenSize => _screenBounds.Size;
    
    protected string DefaultFocusId { private get; set; }

    protected string NextId()
    {
        var value = $"Id{_nextId++}";
        if (string.IsNullOrWhiteSpace(DefaultFocusId))
        {
            DefaultFocusId = value;
        }
        return value;
    }

    IFocusable IPage.FocusedElement
    {
        get => FocusedElement;
        set => _focusWalker.SetFocus(value);
    }

    void IPage.InvalidateRendering()
    {
        _renderingInvalid = true;
    }

    float IPage.Scale => Scale;
    TransitionType IPage.TransitionType { get; set; }
    
    void IPage.SignalRecalculationPending() => _recalculationNeeded = true;

    protected virtual float Scale { get; private set; } = 1;
    
    RectangleF IViewParent.ScreenBounds => _screenBounds;
    
    ViewHandler IPage.RootHandler => _rootHandler;

    TParent IViewParent.GetParent<TParent>(bool optional)
    {
        if (this is TParent parent)
        {
            return parent;
        }

        if (optional)
        {
            return null;
        }
        
        throw new NotSupportedException();
    }
    
    RectangleF IViewParent.CalculateTargetBounds()
    {
        return _bounds;
    }

    void IPage.OnTransitionToFinished() => OnTransitionToFinished();
    bool IPage.MoveFocus(FocusDirection direction) => _focusWalker.MoveFocus(direction);

    protected virtual void OnTransitionToFinished()
    {
    }
    
    public void RecalculateLayout(View view = null)
    {
        Debug.Assert(view == null || view == _rootHandler.View);
        _recalculateLayout = true;

        if (view is IViewParent parent)
        {
            parent.RecalculateLayout();
        }
        _recalculationNeeded = true;
    }

    protected sealed override void Initialize(IUiServices services) => base.Initialize(services);

    void IPage.Load(IUiServices services, IInputContext inputContext, object viewModel)
    {
        var app = (UiApp)services.IoCContainer.Get<IUiApp>();
        _styles = new StylesContainer(app.StylesContainer);
        
        _styles.ParseStyles(GetType());
        _focusWalker = new FocusWalker(this);
        
        ViewModel = (TViewModel)viewModel;
        _iocContainer = services.IoCContainer;

        var root = CreateView();
        _rootHandler = services.HandlerMapper.Create(root, this);
        OnLoad(inputContext);
        _recalculateLayout = true;

        if (!string.IsNullOrWhiteSpace(DefaultFocusId))
        {
            var focusable = inputContext.FindFocusable(DefaultFocusId, this);
            inputContext.Focus(focusable, this);
        }
    }

    protected virtual void OnLoad(IInputContext inputContext)
    {
    }

    void IPage.Update(float dt)
    {
        if (_recalculateLayout)
        {
            _rootHandler.SetBounds(new RectangleF(0, 0,_screenBounds.Width, _screenBounds.Height));
            _recalculateLayout = false;
        }
        
        OnUpdate(dt);

        while (_recalculationNeeded)
        {
            _recalculationNeeded = false;
            _rootHandler.Update(0);
        }
    }

    bool IPage.OnUiButton(UiButton button, IInputContext inputContext)
    {
        if (_transitionProgress > 0)
            return false;
        
        return OnUiButton(button, inputContext);
    }

    protected virtual void OnUpdate(float dt)
    {
        _rootHandler.Update(dt);
    }

    protected virtual bool OnUiButton(UiButton button, IInputContext inputContext)
    {
        if (ViewModel is IUiButtonHandler handler)
        {
            if (handler.OnUiButton(button))
            {
                return true;
            }
        }

        switch (button)
        {
            case UiButton.Back:
                return OnBack();
            
            case UiButton.Menu:
                return OnMenu();
            
            case UiButton.MenuOrBack:
                return OnMenu() || OnBack();
        }

        return false;
    }

    protected virtual bool OnBack()
    {
        if (ViewModel is IPageCommandsSource bcs && (bcs.BackCommand?.CanExecute(null) ?? false))
        {
            bcs.BackCommand.Execute(null);
            return true;
        }
        
        return false;
    }
    
    protected virtual bool OnMenu()
    {
        if (ViewModel is IPageCommandsSource bcs && (bcs.MenuCommand?.CanExecute(null) ?? false))
        {
            bcs.MenuCommand.Execute(null);
            return true;
        }
        
        return false;
    }

    void IPage.Draw(IRenderer renderer) => OnDraw(renderer);
    
    void IPage.SetBounds(RectangleF bounds, float scale) => SetBounds(bounds, scale);
    
    private void SetBounds(RectangleF bounds, float scale)
    {
        Scale = scale;

        _bounds = _screenBounds = new RectangleF(bounds.X * scale, bounds.Y * scale, bounds.Width * scale, bounds.Height * scale);
        _rootHandler.SetBounds(new RectangleF(0,0,_bounds.Width, _bounds.Height));
        
        _recalculationNeeded = true;
        while (_recalculationNeeded)
        {
            _recalculationNeeded = false;
            _rootHandler.Update(0);
        }
    }

    protected virtual void OnDraw(IRenderer renderer)
    {
        _rootHandler.Draw(renderer);

        if (_renderingInvalid)
        {
            _renderingInvalid = false;
            throw new InvalidRenderingException();
        }
    }

    protected abstract View CreateView();

    protected virtual void OnDispose(bool disposing)
    {
        if (disposing)
        {
            _rootHandler.Dispose();
        }
    }
    
    ~Page()
    {
        OnDispose(false);
    }
    
    void IDisposable.Dispose()
    {
        OnDispose(true);
    }
}