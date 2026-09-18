using System.Numerics;
using Ssit.CrossX2.Content;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Sprites;
using Ssit.CrossX2.UI.Parameters;
using Ssit.CrossX2.UI.Views;

namespace Ssit.CrossX2.UI.Handlers;

public class SpriteViewHandler : ViewHandler<SpriteView>
{
    private readonly ResourceHandle<SpriteEx> _spriteObject;
    private readonly SpriteInstance _spriteInstance;

    public SpriteViewHandler(CreateHandlerParameters parameters, IContentManager contentManager) : base(parameters)
    {
        _spriteObject = contentManager.Get<SpriteEx>(AttachedView.SpritePath);
        _spriteInstance = _spriteObject.Resource.CreateSpriteInstance();

        if (AttachedView.Sequence != null)
        {
            ApplySequence();
            AttachedView.Sequence.TextChanged += ApplySequence;
        }
    }

    private void ApplySequence()
    {
        var sequence = AttachedView.Sequence?.ToString();
        if (!string.IsNullOrEmpty(sequence))
        {
            _spriteInstance.SetSequence(sequence);
        }
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        _spriteInstance.Advance(dt * AttachedView.SpeedFactor);
    }

    protected override void OnDraw(IRenderer renderer)
    {
        var scale = CurrentScale;
        var x = AttachedView.ImageAnchorX?.Calculate(scale, ScreenBounds.Width) ?? 0;
        var y = AttachedView.ImageAnchorY?.Calculate(scale, ScreenBounds.Height) ?? 0;
        
        renderer.StateManager.SetTextureFilter(TextureFilter.Point);
        
        renderer.SpriteRenderer.Draw(_spriteInstance, ScreenBounds.TopLeft + new Vector2(x, y), scale * AttachedView.Scale);
    }

    public override void CalculateSize(out Length width, out Length height)
    {
        width = AttachedView.Width ?? new Length(pixels: _spriteInstance.Source.Width);
        height = AttachedView.Height ?? new Length(pixels: _spriteInstance.Source.Height);
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);
        
        if (AttachedView.Sequence != null)
            AttachedView.Sequence.TextChanged -= ApplySequence;
        
        _spriteInstance?.Dispose();
        _spriteObject?.Dispose();
    }
}
