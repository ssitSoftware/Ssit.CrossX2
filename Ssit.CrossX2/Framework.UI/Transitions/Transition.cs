using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;

namespace Ssit.CrossX2.Framework.UI.Transitions;

public abstract class Transition : ITransition
{
    public TransitionType ForTransitions { get; set; }
    
    public float Power { get; set; } = 1;
    public float ProgressMin { get; set; } = 0;
    public float ProgressMax { get; set; } = 1;
    
    // Number of applies not finished yet - a transition can be applied twice in one draw (a page transition and
    // a Show / Hide transition of the view).
    private int _applied;
    
    void ITransition.Apply(ViewHandler handler, IRenderer renderer, float scale, TransitionType type, float progress)
    {
        if ((ForTransitions & type) == 0)
            return;
        
        renderer.StateManager.SaveState();
        
        progress = MathF.Max(0, Math.Min(1, (progress - ProgressMin) / (ProgressMax - ProgressMin)));
        progress = MathF.Pow(progress, Power);

        OnApply(handler, renderer, scale, progress);
        _applied++;
    }

    void ITransition.Finish(IRenderer renderer)
    {
        if (_applied == 0)
            return;
        
        renderer.StateManager.RestoreState();
        _applied--;
    }
    
    protected abstract void OnApply(ViewHandler handler, IRenderer renderer, float scale, float progress);
}