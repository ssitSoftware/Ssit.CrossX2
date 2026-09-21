using System.Numerics;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Common.Pages;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class HorizontalSliderHandler<TSlider> : ViewHandler<TSlider>, IUiCommandHandler, IInputConsumer where TSlider: HorizontalSlider, new()
{
    private readonly PageInputContext _pageInputContext;
    private readonly ResourceHandle<DualTexture> _texture;
    private readonly IColorSource _colorSource;
    private int? _currentPointerId;
    
    public HorizontalSliderHandler(CreateHandlerParameters parameters, IContentManager contentManager, PageInputContext pageInputContext) : base(parameters)
    {
        _pageInputContext = pageInputContext;
        _texture = contentManager.Get<DualTexture>(AttachedView.Template.Path);
        _colorSource = parameters.Parent?.GetParent<IColorSource>(true);
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);
        _texture?.Dispose();
    }

    public override void CalculateSize(out Length width, out Length height)
    {
        width = AttachedView.Width ?? Length.Fill;
        height = AttachedView.Height ?? Length.Auto;
        
        if (height.IsAuto)
        {
            height = new Length(AttachedView.Template.Thumb.Height);
        }

        if (width.IsAuto)
        {
            throw new NotSupportedException("HorizontalSlider width cannot be auto!");
        }
    }

    protected override void OnDraw(IRenderer renderer)
    {
        var sb = ScreenBounds;

        var offset = AttachedView.Value.Value - AttachedView.Min;
        var floatOffset = (float) offset / (AttachedView.Max - AttachedView.Min);
        
        var thumbSize = AttachedView.Template.Thumb.Size.ToVector() * CurrentScale;
        var totalWidth = sb.Width - thumbSize.X;

        var fgColor =
            AttachedView.ForegroundColor?.GetColor(renderer, _colorSource) ?? RgbaColor.White;
        
        var outlineColor =
            AttachedView.OutlineColor?.GetColor(renderer, _colorSource) ?? RgbaColor.Black;
        
        var thumbRect = new RectangleF(sb.X + floatOffset * totalWidth, sb.Center.Y - thumbSize.Y / 2, thumbSize.X, thumbSize.Y);

        var posX = sb.X;
        var posY = sb.Center.Y - AttachedView.Template.Track.Height * CurrentScale / 2f;
        
        renderer.SpriteRenderer.Draw(_texture.Resource.Texture, new Vector2(posX, posY), AttachedView.Template.TrackStart, origin: Vector2.Zero, scale: CurrentScale, color: fgColor); 
        renderer.SpriteRenderer.Draw(_texture.Resource.Outline, new Vector2(posX, posY), AttachedView.Template.TrackStart, origin: Vector2.Zero, scale: CurrentScale, color: outlineColor);
        
        posX += AttachedView.Template.TrackStart.Width * CurrentScale;
        
        var trackLength = sb.Width - (AttachedView.Template.TrackStart.Width + AttachedView.Template.TrackEnd.Width) * CurrentScale;
        
        renderer.SpriteRenderer.Draw(_texture.Resource.Texture, new RectangleF(posX, posY, trackLength, AttachedView.Template.Track.Height * CurrentScale), AttachedView.Template.Track, nullableColor: fgColor); 
        renderer.SpriteRenderer.Draw(_texture.Resource.Outline, new RectangleF(posX, posY, trackLength, AttachedView.Template.Track.Height * CurrentScale), AttachedView.Template.Track, nullableColor: outlineColor);

        posX += trackLength;
        
        renderer.SpriteRenderer.Draw(_texture.Resource.Texture, new Vector2(posX, posY), AttachedView.Template.TrackEnd, origin: Vector2.Zero, scale: CurrentScale, color: fgColor); 
        renderer.SpriteRenderer.Draw(_texture.Resource.Outline, new Vector2(posX, posY), AttachedView.Template.TrackEnd, origin: Vector2.Zero, scale: CurrentScale, color: outlineColor);
        
        renderer.SpriteRenderer.Draw(_texture.Resource.Texture, thumbRect, AttachedView.Template.Thumb, nullableColor: fgColor);
        renderer.SpriteRenderer.Draw(_texture.Resource.Outline, thumbRect, AttachedView.Template.Thumb, nullableColor: outlineColor);
    }

    public bool OnUiButton(UiButton button, IInputContext context)
    {
        switch (button)
        {
            case UiButton.Left:
                if (AttachedView?.Value is not null)
                {
                    var value = AttachedView.Value.Value;
                    value = Math.Max(AttachedView.Min, Math.Min(value - 1, AttachedView.Max));
                    AttachedView.Value.Value = value;
                    return true;
                }
                break;

            case UiButton.Right:
                if (AttachedView?.Value is not null)
                {
                    var value = AttachedView.Value.Value;
                    value = Math.Max(AttachedView.Min, Math.Min(value + 1, AttachedView.Max));
                    AttachedView.Value.Value = value;
                    return true;
                }
                break;
        }
        return false;
    }

    public void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context)
    {
    }

    public bool ProcessInput(IReadOnlyList<Pointer> pointers, IInputContext context)
    {
        if (AttachedView?.Value is null) return false;

        if (_currentPointerId.HasValue)
        {
            var pointer = pointers.FirstOrDefault(o => o.Id == _currentPointerId.Value);
            if (pointer != null)
            {
                UpdateValueFromPosition(pointer.Position.X);

                if (pointer.State == ButtonState.JustReleased || pointer.State == ButtonState.Empty)
                {
                    _currentPointerId = null;
                }
                return true;
            }
            _currentPointerId = null;
        }

        foreach (var pointer in pointers)
        {
            if (pointer.State == ButtonState.JustPressed && ScreenBounds.Contains(pointer.Position))
            {
                _currentPointerId = pointer.Id;
                context.CapturePointer(pointer.Id, this);
                UpdateValueFromPosition(pointer.Position.X);

                _pageInputContext.ShowFocus = false;
                
                var focusable = Parent.GetParent<IFocusable>(true);
                if (focusable is not null)
                {
                    context.Focus(focusable, this);
                }
                return true;
            }
        }

        return false;
    }

    public void CancelPointer(int pointerId, IInputContext context)
    {
        if (_currentPointerId == pointerId)
        {
            _currentPointerId = null;
        }
    }

    private void UpdateValueFromPosition(float pointerX)
    {
        var sb = ScreenBounds;
        var thumbWidth = AttachedView.Template.Thumb.Width * CurrentScale;
        var totalWidth = sb.Width - thumbWidth;

        if (totalWidth <= 0) return;

        var floatOffset = Math.Clamp((pointerX - sb.X - thumbWidth / 2f) / totalWidth, 0f, 1f);
        var range = AttachedView.Max - AttachedView.Min;
        AttachedView.Value.Value = AttachedView.Min + (int)Math.Round(floatOffset * range);
    }
}