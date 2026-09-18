using System.Numerics;
using Ssit.CrossX2.Graphics;

namespace Ssit.CrossX2.UI.Transitions;

public class TranslationTransition: Transition
{
    public Vector2 Offset { get; init; }
    
    protected override void OnApply(IRenderer renderer, float scale, float progress)
    {
        var offset = Offset * progress;
        renderer.StateManager.Translate(offset * scale);
    }
}