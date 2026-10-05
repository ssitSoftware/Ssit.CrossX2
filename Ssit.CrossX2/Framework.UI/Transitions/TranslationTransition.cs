using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;

namespace Ssit.CrossX2.Framework.UI.Transitions;

public class TranslationTransition: Transition
{
    public Vector2 Offset { get; init; }
    
    protected override void OnApply(ViewHandler _, IRenderer renderer, float scale, float progress)
    {
        var offset = Offset * progress;
        renderer.StateManager.Translate(offset * scale);
    }
}