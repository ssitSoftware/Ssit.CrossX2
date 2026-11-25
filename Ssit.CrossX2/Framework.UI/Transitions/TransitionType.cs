namespace Ssit.CrossX2.Framework.UI.Transitions;

[Flags]
public enum TransitionType
{
    None = 0,
    NavigateTo = 1,
    NavigateFrom = 2,
    NavigateBackTo = 4,
    NavigateBackFrom = 8,
    Navigation = NavigateTo | NavigateBackTo | NavigateFrom | NavigateBackFrom,
    /// <summary>
    /// The view became visible (<see cref="Views.View.Visible"/> changed to true) - progress goes from 1 to 0.
    /// </summary>
    Show = 16,

    /// <summary>
    /// The view became hidden (<see cref="Views.View.Visible"/> changed to false) - progress goes from 0 to 1.
    /// </summary>
    Hide = 32,
    ShowHide = Show | Hide,
}