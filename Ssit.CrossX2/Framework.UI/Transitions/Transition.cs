using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.UI.Transitions;

public abstract class Transition : ITransition
{
    public TransitionType ForTransitions { get; set; }
    
    public float Power { get; set; } = 1;
    public float ProgressMin { get; set; } = 0;
    public float ProgressMax { get; set; } = 1;
    
    private bool _applied;
    
    void ITransition.Apply(IRenderer renderer, float scale, TransitionType type, float progress)
    {
        if ((ForTransitions & type) == 0)
            return;
        
        renderer.StateManager.SaveState();
        
        progress = MathF.Max(0, Math.Min(1, (progress - ProgressMin) / (ProgressMax - ProgressMin)));
        progress = MathF.Pow(progress, Power);
        
        OnApply(renderer, scale, progress);

        _applied = true;
    }

    void ITransition.Finish(IRenderer renderer)
    {
        if (!_applied)
            return;
        
        renderer.StateManager.RestoreState();
        _applied = false;
    }
    
    protected abstract void OnApply(IRenderer renderer, float scale, float progress);
}