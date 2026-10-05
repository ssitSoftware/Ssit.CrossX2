using Ssit.CrossX2.Framework.Games.Services;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;

namespace Ssit.CrossX2.Framework.Games.UI;

internal class GameViewHandler : BackgroundHandler<GameView>
{
    private readonly IGameDebugService _gameDebugService;

    public GameViewHandler(CreateHandlerParameters parameters, IGameDebugService gameDebugService = null) : base(parameters)
    {
        _gameDebugService = gameDebugService;
        AttachedView.Active.ValueChanged += ActiveOnValueChanged;
    }

    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer);
        
        var gameInstance = AttachedView.GameInstance;
        for (var pass = 0; pass < gameInstance.RenderPasses; pass++)
        {
            gameInstance.Render(renderer, ScreenBounds, pass, CurrentScale);
        }

        if (AttachedView.ShowDebug?.Value ?? _gameDebugService?.ShowDebug?.Value ?? false)
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

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);
        AttachedView.Active.ValueChanged -= ActiveOnValueChanged;
    }
}