#if FALSE
using Ssit.CrossX2.Graphics;

namespace Ssit.CrossX2.UI.Handlers;

internal class GameViewHandler(ViewHandler.CreateHandlerParameters parameters) : BackgroundHandler<GameView>(parameters)
{
    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer);
        
        AttachedView.Active.ValueChanged += ActiveOnValueChanged;
        
        var gameInstance = AttachedView.GameInstance;
        for (var pass = 0; pass < gameInstance.RenderPasses; pass++)
        {
            gameInstance.Render(renderer, ScreenBounds, pass, CurrentScale);
        }

        if (AttachedView.ShowDebug?.Value ?? false)
        {
            gameInstance.RenderDebug(renderer, ScreenBounds, CurrentScale);
        }
        
        gameInstance.Activate(AttachedView.Active.Value);
    }

    private void ActiveOnValueChanged()
    {
        var gameInstance = AttachedView.GameInstance;
        gameInstance.Activate(AttachedView.Active.Value);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        
        if (dt > 0 && (AttachedView.Active?.Value ?? true))
        {
            var gameInstance = AttachedView.GameInstance;
            gameInstance.Update(dt * AttachedView.SpeedFactor);
        }
    }
}

#endif