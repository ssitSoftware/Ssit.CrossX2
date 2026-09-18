namespace Ssit.CrossX2.UI.Services;

public interface INavigation
{
    void ClearNavigateToSerie(params Type[] viewModelTypes);
    void ClearNavigateTo<TViewModel>(object parameter = null) where TViewModel : class;
    void NavigateTo<TViewModel>(object parameter = null, bool skipTransition = false) where TViewModel : class;
    void NavigateBack();
    void NavigateBackTo<TViewModel>() where TViewModel : class;
    int NavigationStackCount { get; }
    bool ParallelTransitions { get; set; }
    bool IsOnTop(object vmOrPage);
}