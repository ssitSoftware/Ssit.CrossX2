using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.UI.Handlers;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Transitions;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI;

internal interface IPage: IViewParent, IDisposable
{
    object ViewModel { get; }
    void Load(IUiServices services, IInputContext inputContext, object viewModel);
    void Update(float dt);
    void Draw(IRenderer renderer);
    void SetBounds(RectangleF bounds, float scale);
    ViewHandler RootHandler { get; }
    bool OnUiButton(UiButton button, IInputContext inputProcessor);
    IFocusable FocusedElement { get; set; }
    float TransitionTime { get; }
    float TransitionProgress { get; set; }
    float Scale { get; }
    TransitionType TransitionType { get; set; }
    void SignalRecalculationPending();
    void InvalidateRendering();
    void OnTransitionToFinished();
    bool MoveFocus(FocusDirection direction);
    StylesContainer Styles { get; }
}