namespace Ssit.CrossX2.UI.Transitions;

[Flags]
public enum TransitionType
{
    NavigateTo = 1,
    NavigateFrom = 2,
    NavigateBackTo = 4,
    NavigateBackFrom = 8,
    Navigation = NavigateTo | NavigateBackTo | NavigateFrom | NavigateBackFrom,
    Show = 8,
    Hide = 16,
    ShowHide = Show | Hide,
}