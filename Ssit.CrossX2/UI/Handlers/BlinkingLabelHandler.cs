using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Font;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Views;

namespace Ssit.CrossX2.UI.Handlers;

public class BlinkingLabelHandler<TLabel>: LabelHandler<TLabel> where TLabel: Label, IBlinkingView
{
    private float _currentTime = 0;

    private float VisibleTime => AttachedView.VisibleTime ?? 1f;
    private float HiddenTime => AttachedView.HiddenTime ?? 1f;
    
    public BlinkingLabelHandler(CreateHandlerParameters parameters, IFontsManager fontsManager, IUiActionDispatcher uiActionDispatcher) : base(parameters, fontsManager, uiActionDispatcher)
    {
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        _currentTime += dt;
        _currentTime %= VisibleTime + HiddenTime;
    }

    public override void Draw(IRenderer renderer)
    {
        if (_currentTime < VisibleTime)
        {
            base.Draw(renderer);
        }
    }
}