using System.Reflection;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.UI.Transitions;

namespace Ssit.CrossX2.Framework.UI.Services;

internal class Navigation: INavigation
{
    private class StackData
    {
        public object ViewModel;
        public string FocusedId;

        public StackData(object viewModel)
        {
            ViewModel = viewModel;
            FocusedId = null;
        }
    }
    
    private readonly NavigationMap _navigationMap;
    private readonly IIoCContainer _iocContainer;
    private readonly IUiServices _uiServices;
    private readonly UiApp _uiApp;

    private readonly Stack<StackData> _navigationStack = new();
    private readonly List<IDisposable> _objectsToDisposeOnTransitionFinished = new();
    
    public IPage PreviousPage { get; private set; }
    public IPage CurrentPage { get; private set; }
    public bool PreviousPageOnTop { get; private set; }
    
    public int NavigationStackCount => _navigationStack.Count;
    public bool ParallelTransitions { get; set; }
    public bool IsOnTop(object vmOrPage)
    {
        var top = _navigationStack.Peek();
        if (top != null)
        {
            return ReferenceEquals(top.ViewModel, vmOrPage) || ReferenceEquals(CurrentPage, vmOrPage);
        }

        return false;
    }

    public Navigation(NavigationMap navigationMap, IIoCContainer iocContainer, IUiServices uiServices, IUiApp uiApp)
    {
        _navigationMap = navigationMap;
        _iocContainer = iocContainer;
        _uiServices = uiServices;
        _uiApp = uiApp as UiApp;
    }

    public void ClearNavigateToSerie(params Type[] types)
    {
        if (types.Length == 0)
            throw new InvalidOperationException();
        
        foreach (var data in _navigationStack)
        {
            if (data.ViewModel is IDisposable disposable)
            {
                _objectsToDisposeOnTransitionFinished.Add(disposable);
            }
        }
        _navigationStack.Clear();

        object vm = null;
        foreach (var type in types)
        {
            vm = _iocContainer.IoCConstruct(type);
            _navigationStack.Push(new StackData(vm));
        }

        InitializePageNavigation(vm);
        
        if (PreviousPage != null)
        {
            PreviousPage.TransitionProgress = 0.001f;
            PreviousPage.TransitionType = TransitionType.NavigateBackFrom;
        }
        CurrentPage.TransitionProgress = 1;
        CurrentPage.TransitionType = TransitionType.NavigateBackTo;
    }
    
    public void ClearNavigateTo<TViewModel>(object parameter = null) where TViewModel : class
    {
        foreach (var data in _navigationStack)
        {
            if (data.ViewModel is IDisposable disposable)
            {
                _objectsToDisposeOnTransitionFinished.Add(disposable);
            }
        }
        _navigationStack.Clear();
        NavigateTo<TViewModel>(parameter);
    }

    private void InitializePageNavigation(object vm, bool skipTransition = false)
    {
        var page = InitializePage(vm, null);
        PreviousPage = CurrentPage;
        CurrentPage = page;

        if (PreviousPage != null)
        {
            PreviousPage.TransitionProgress = skipTransition ? 0.99999999f : 0.001f;
            PreviousPage.TransitionType = TransitionType.NavigateFrom;
        }
        
        CurrentPage.TransitionProgress = skipTransition ? 0.00001f : 1;
        CurrentPage.TransitionType = TransitionType.NavigateTo;
        
        PreviousPageOnTop = false;

        if (skipTransition)
        {
            Update(0.1f);
        }
    }

    public void NavigateTo<TViewModel>(object parameter = null, bool skipTransition = false) where TViewModel : class
    {
        if (_navigationStack.Count > 0)
        {
            var data = _navigationStack.Peek();
            data.FocusedId = CurrentPage.FocusedElement?.UniqueId;
        }
        
        var vm = _iocContainer.IoCConstruct<TViewModel>(parameter);
        _navigationStack.Push(new StackData(vm));
        
        InitializePageNavigation(vm, skipTransition);
    }

    public void NavigateBack()
    {
        if (_navigationStack.Count == 1)
        {
            throw new InvalidOperationException("Cannot navigate back when the navigation stack is empty.");
        }

        var oldData = _navigationStack.Pop();

        if (oldData.ViewModel is IDisposable disposable)
        {
            _objectsToDisposeOnTransitionFinished.Add(disposable);
        }

        while (_navigationStack.Peek().ViewModel.GetType().GetCustomAttribute(typeof(NoHistoryAttribute)) != null)
        {
            (_navigationStack.Pop().ViewModel as IDisposable)?.Dispose();
        }

        var data = _navigationStack.Peek();
        var page = InitializePage(data.ViewModel, data.FocusedId);
        
        PreviousPage = CurrentPage;
        CurrentPage = page;
        PreviousPageOnTop = true;
        
        if (PreviousPage != null)
        {
            PreviousPage.TransitionProgress = 0.001f;
            PreviousPage.TransitionType = TransitionType.NavigateBackFrom;
        }
        CurrentPage.TransitionProgress = 1;
        CurrentPage.TransitionType = TransitionType.NavigateBackTo;

        if (data.ViewModel is INavigationEventHandler handler)
        {
            handler.OnNavigatedBackTo();
        }
    }

    public void NavigateBackTo<TViewModel>() where TViewModel : class
    {
        if (_navigationStack.Count == 1)
        {
            throw new InvalidOperationException("Cannot navigate back when the navigation stack is empty.");
        }
        
        var oldData = _navigationStack.Pop();
        
        if (oldData.ViewModel is IDisposable disposable2)
        {
            _objectsToDisposeOnTransitionFinished.Add(disposable2);
        }
        
        var data = _navigationStack.Peek();
        
        while (_navigationStack.Count > 1)
        {
            if (data.ViewModel is TViewModel) break;

            if (data.ViewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _navigationStack.Pop();
            data = _navigationStack.Peek();
        }
        
        var page = InitializePage(data.ViewModel, data.FocusedId);
        
        PreviousPage = CurrentPage;
        CurrentPage = page;
        PreviousPageOnTop = true;
        
        if (PreviousPage != null)
        {
            PreviousPage.TransitionProgress = 0.001f;
            PreviousPage.TransitionType = TransitionType.NavigateBackFrom;
        }
        CurrentPage.TransitionProgress = 1;
        CurrentPage.TransitionType = TransitionType.NavigateBackTo;
    }

    private IPage InitializePage(object vm, string focusedId)
    {
        var pageType = _navigationMap.GetPageTypeFromViewModel(vm);

        var page = (IPage)_iocContainer.IoCConstruct(pageType);
        if (page is null) throw new InvalidProgramException();
        
        page.Load(_uiServices, _uiApp.InputProcessor, vm);
        page.SetBounds(_uiApp.Bounds, _uiApp.Scale);
        
        if (focusedId != null)
        {
            var focusable = _uiApp.InputProcessor.FindFocusable(focusedId, page);
            if (focusable != null)
            {
                _uiApp.InputProcessor.Focus(focusable, page);
            }
        }
        
        return page;
    }

    public void Update(float dt)
    {
        if (PreviousPage?.TransitionProgress < 1)
        {
            PreviousPage.Update(dt);
            
            var transitionTime = MathF.Max(PreviousPage.TransitionTime, 0.0011f);
            PreviousPage.TransitionProgress += dt * 1 / transitionTime;
            
            if (PreviousPage?.TransitionProgress >= 1)
            {
                PreviousPage?.Dispose();
                PreviousPage = null;
            }
        }

        if (ParallelTransitions || PreviousPage is null)
        {
            if (CurrentPage?.TransitionProgress > 0)
            {
                var transitionTime = MathF.Max(CurrentPage.TransitionTime, 0.0011f);
                CurrentPage.TransitionProgress -= dt * 1 / transitionTime;

                if (CurrentPage?.TransitionProgress <= 0)
                {
                    CurrentPage.OnTransitionToFinished();
                    CurrentPage.TransitionProgress = 0;
                    CurrentPage.TransitionType = 0;
                }
            }
            
            CurrentPage?.Update(dt);
        }

        if (PreviousPage is null && _objectsToDisposeOnTransitionFinished.Count > 0)
        {
            foreach (var disposable in _objectsToDisposeOnTransitionFinished)
            {
                disposable?.Dispose();
            }
            _objectsToDisposeOnTransitionFinished.Clear();
        }
    }
}