using System.Numerics;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.Input.Internal;
using Ssit.CrossX2.Framework.UI.Common.Pages;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class HorizontalSliderHandler<TSlider> : ViewHandler<TSlider>, IUiCommandHandler, IInputConsumer where TSlider: HorizontalSlider, new()
{
    private readonly PageInputContext _pageInputContext;
    private readonly IPointingDevices _pointingDevices;
    private readonly ResourceHandle<DualTexture> _texture;
    private readonly IColorSource _colorSource;
    private int? _currentPointerId;
    
    public HorizontalSliderHandler(CreateHandlerParameters parameters, IContentManager contentManager, PageInputContext pageInputContext, IPointingDevices pointingDevices) : base(parameters)
    {
        _pageInputContext = pageInputContext;
        _pointingDevices = pointingDevices;
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

    /// <summary>
    /// Bounds of the track with its outline: the screen bounds extended by <see cref="HorizontalSlider.WithOutlineWidth"/>
    /// on both sides, so the outline baked into the graphics lies outside the slider bounds (like a text outline).
    /// </summary>
    private RectangleF TrackBounds
    {
        get
        {
            var sb = ScreenBounds;
            var outlineWidth = AttachedView.WithOutlineWidth * CurrentScale;
            return new RectangleF(sb.X - outlineWidth, sb.Y, sb.Width + outlineWidth * 2, sb.Height);
        }
    }

    protected override void OnDraw(IRenderer renderer)
    {
        var sb = TrackBounds;

        var offset = AttachedView.Value.Value - AttachedView.Min;
        var floatOffset = (float) offset / (AttachedView.Max - AttachedView.Min);
        
        var thumbSize = AttachedView.Template.Thumb.Size.ToVector() * CurrentScale;
        var totalWidth = sb.Width - thumbSize.X;

        var fgColor =
            AttachedView.ForegroundColor?.GetColor(renderer, _colorSource) ?? RgbaColor.White;
        
        var outlineColor =
            AttachedView.OutlineColor?.GetColor(renderer, _colorSource) ?? RgbaColor.Black;
        
        // All parts are snapped to whole screen pixels and the track parts are drawn edge to edge. With a fractional
        // position (e.g. the track moved by an odd WithOutlineWidth) the pixel centres can fall exactly on texel edges,
        // and the end quads and the stretched middle quad then picked different texture rows - the track ends
        // showed 1 px higher than the middle.
        var thumbRect = new RectangleF(MathF.Round(sb.X + floatOffset * totalWidth), MathF.Round(sb.Center.Y - thumbSize.Y / 2), thumbSize.X, thumbSize.Y);

        var trackHeight = AttachedView.Template.Track.Height * CurrentScale;
        var posY = MathF.Round(sb.Center.Y - trackHeight / 2f);

        var startX = MathF.Round(sb.X);
        var middleX = MathF.Round(sb.X + AttachedView.Template.TrackStart.Width * CurrentScale);
        var endX = MathF.Round(sb.Right - AttachedView.Template.TrackEnd.Width * CurrentScale);
        var rightX = MathF.Round(sb.Right);
        var trackLength = endX - middleX;
        
        renderer.SpriteRenderer.Draw(_texture.Resource.Texture, new RectangleF(startX, posY, middleX - startX, trackHeight), AttachedView.Template.TrackStart, nullableColor: fgColor); 
        renderer.SpriteRenderer.Draw(_texture.Resource.Outline, new RectangleF(startX, posY, middleX - startX, trackHeight), AttachedView.Template.TrackStart, nullableColor: outlineColor);
        
        renderer.SpriteRenderer.Draw(_texture.Resource.Texture, new RectangleF(middleX, posY, trackLength, trackHeight), AttachedView.Template.Track, nullableColor: fgColor); 
        renderer.SpriteRenderer.Draw(_texture.Resource.Outline, new RectangleF(middleX, posY, trackLength, trackHeight), AttachedView.Template.Track, nullableColor: outlineColor);
        
        renderer.SpriteRenderer.Draw(_texture.Resource.Texture, new RectangleF(endX, posY, rightX - endX, trackHeight), AttachedView.Template.TrackEnd, nullableColor: fgColor); 
        renderer.SpriteRenderer.Draw(_texture.Resource.Outline, new RectangleF(endX, posY, rightX - endX, trackHeight), AttachedView.Template.TrackEnd, nullableColor: outlineColor);
        
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

                _pageInputContext.ShowFocus = _pointingDevices.Mode.HasFlag(PointingDevicesMode.AutoHideMouse);
                
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
        var sb = TrackBounds;
        var thumbWidth = AttachedView.Template.Thumb.Width * CurrentScale;
        var totalWidth = sb.Width - thumbWidth;

        if (totalWidth <= 0) return;

        var floatOffset = Math.Clamp((pointerX - sb.X - thumbWidth / 2f) / totalWidth, 0f, 1f);
        var range = AttachedView.Max - AttachedView.Min;
        AttachedView.Value.Value = AttachedView.Min + (int)Math.Round(floatOffset * range);
    }
}