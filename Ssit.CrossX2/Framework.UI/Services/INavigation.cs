namespace Ssit.CrossX2.Framework.UI.Services;

public interface INavigation
{
    void ClearNavigateToSerie(params Type[] viewModelTypes);
    void ClearNavigateTo<TViewModel>(object parameter = null) where TViewModel : class;
    void NavigateTo<TViewModel>(object parameter = null, bool skipTransition = false, bool navigationSound = true) where TViewModel : class;
    void NavigateBack();
    void NavigateBackTo<TViewModel>() where TViewModel : class;

    /// <summary>
    /// Removes view models from the top of the navigation stack until <typeparamref name="TViewModel"/> is on top.
    /// The current page is not changed - it is treated as the previous page by the next navigation.
    /// Removed view models are not disposed immediately; they are disposed after the next page transition finishes,
    /// so the next navigation has to be started in the same frame.
    /// </summary>
    void ClearStackTo<TViewModel>() where TViewModel : class;
    int NavigationStackCount { get; }
    bool ParallelTransitions { get; set; }
    bool IsOnTop(object vmOrPage);
}