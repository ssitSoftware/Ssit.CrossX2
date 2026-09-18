using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Font;
using Ssit.CrossX2.UI.Components;
using Ssit.CrossX2.UI.Values;
using Ssit.CrossX2.UI.Views;

namespace Ssit.CrossX2.UI.Handlers;

public class StopwatchControlHandler : ViewHandler<StopwatchControl>
{
    private readonly StopwatchComponent _stopwatch;
    private readonly StopwatchComponentParameters _componentParameters = new();

    public StopwatchControlHandler(CreateHandlerParameters parameters, IFontsManager fontsManager)
        : base(parameters)
    {
        _stopwatch = new StopwatchComponent(fontsManager);
        _stopwatch.ComponentParameters = _componentParameters;
        
        _componentParameters.Font = AttachedView.Font;
        _componentParameters.TextColor = AttachedView.TextColor;
        _componentParameters.OutlineColor = AttachedView.OutlineColor;
        _componentParameters.Scaling = AttachedView.Scaling;
        _componentParameters.TimeTimeElements = AttachedView.TimeTimeElements;
        _componentParameters.StartTime = AttachedView.StartTime;
        _componentParameters.Padding = AttachedView.Padding ?? Thickness.Zero;
        _componentParameters.Align = AttachedView.Align ?? ContentAlign.Center | ContentAlign.VCenter;
        _componentParameters.ShouldDisplay = AttachedView?.Visible ?? true;
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (AttachedView.Visible?.Value != true)
        {
            _stopwatch.Reset();
            return;
        }

        _stopwatch.Update();
    }

    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer);
        _stopwatch.Draw(renderer, ScreenBounds, CurrentScale);
    }
}
