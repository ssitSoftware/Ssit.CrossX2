using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Graphics.Renderers;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.Text;
using Ssit.CrossX2.Framework.UI.Common.Pages;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class TextInputHandler(
    ViewHandler.CreateHandlerParameters parameters,
    IFontsManager fontsManager,
    PageInputContext pageInputContext,
    IUiSounds uiSounds,
    INativeTextInputService nativeTextInputService,
    IInputCoordinateSystem inputCoordinateSystem)
    : ViewHandler<TextInput>(parameters), IFocusable, IInputConsumer, INativeTextInputConsumer
{
    public bool Focused { get; private set; }
    public bool DisableAllInput => false;

    public bool Enabled => AttachedView.Enabled?.Value ?? true;

    private bool _isActiveInput;
    private bool _hovered;
    private bool _pushed;
    private int? _currentPointerId;
    private float _scale;
    
    private  INativeTextInput _currentTextInput;
    
    private int _cursorPosition;
    private int _selectionStart = -1;

    protected float TextScale => AttachedView.Scaling == TextScaling.Pixel ? CurrentScale : _scale;

    private readonly TextRenderingContext _textRenderingContext = new();
    
    private string _currentText;
    private bool _wasActiveInput;
    private float _textDisplayOffset;
    
    public override void Init()
    {
        base.Init();
        _currentText = AttachedView.Text?.ToString() ?? "";
    }

    public bool OnUiButton(UiButton button, IInputContext context)
    {
        if (_wasActiveInput)
        {
            if (button is UiButton.Back or UiButton.MenuOrBack)
            {
                Deactivate();
                return true;
            }

            return false;
        }
        
        FocusDirection focusDirection = FocusDirection.None;
        
        switch (button)
        {
            case UiButton.Left:
                focusDirection = FocusDirection.Left;
                break;

            case UiButton.Right:
                focusDirection = FocusDirection.Right;
                break;

            case UiButton.Up:
                focusDirection = FocusDirection.Up;
                break;

            case UiButton.Down:
                focusDirection = FocusDirection.Down;
                break;

            case UiButton.Select:
                if (Enabled)
                {
                    if (!pageInputContext.ShowFocus)
                    {
                        uiSounds[UiSounds.ItemNavigateSound]?.PlayOnce();
                        pageInputContext.ShowFocus = true;
                        return false;
                    }
                    (uiSounds[UiSounds.ExecuteSound] ?? uiSounds[UiSounds.ButtonReleaseSound])?.PlayOnce();
                    Activate();
                }
                break;
        }

        if (focusDirection != FocusDirection.None)
        {
            if (!pageInputContext.ShowFocus)
            {
                uiSounds[UiSounds.ItemNavigateSound]?.PlayOnce();
                pageInputContext.ShowFocus = true;
                context.Focus(this, this);
                return true;
            }

            if (context.MoveFocus(focusDirection, this))
            {
                uiSounds[UiSounds.ItemNavigateSound]?.PlayOnce();
            }
        }

        return false;
    }

    private void Activate(Vector2 position)
    {
        Activate();
        
        var posX = position.X - ScreenBounds.X;
        if (AttachedView.Padding?.Left.HasValue ?? false)
        {
            var pad = AttachedView.Padding.Value.Left.Value.Calculate(CurrentScale, ScreenBounds.Width);
            posX -= pad;
        }
         
        var font = GetFont();
        
        _cursorPosition = _currentText.Length;
        
        for(var idx = 0; idx < _currentText.Length; idx++)
        {
            var size1 = font.TextSize(new TextSource(_currentText, 0, idx)).Width * TextScale;
            var size2 = font.TextSize(new TextSource(_currentText, 0, idx+1)).Width * TextScale;
            if (posX < (size1+size2) / 2f)
            {
                _cursorPosition = idx;
                break;
            }
        }
    }

    private void Activate()
    {
        if (_currentTextInput != null)
        {
            _currentTextInput.Reactivate();
            UpdateNativePosition();
            return;
        }

        _cursorPosition = _currentText.Length;
        _isActiveInput = true;
        
        _currentTextInput = nativeTextInputService.AllocateTextInput(this, InputType.Text);
        UpdateNativePosition();
    }

    private void UpdateNativePosition()
    {
        if (_currentTextInput is null)
            return;

        var rect = CalculateNativePosition();
        _currentTextInput.UpdatePosition(rect, 0);
    }

    private RectangleF CalculateNativePosition()
    {
        var sb = ScreenBounds;

        var frameWidth = AttachedView.ActiveFrameThickness?.Calculate(CurrentScale, 1) ?? CurrentScale + 1;

        sb = sb.Inflate(frameWidth);
        var margin = AttachedView.AdditionalKeyboardMargin?.Calculate(CurrentScale, 1) ?? 0;
        
        sb = new RectangleF(sb.X, sb.Y - margin, sb.Width, sb.Height + margin * 2);
        
        var topLeft = Vector2.Transform(sb.TopLeft, inputCoordinateSystem.TransformInv);
        var bottomRight = Vector2.Transform(sb.BottomRight, inputCoordinateSystem.TransformInv);

        return new RectangleF(topLeft, bottomRight - topLeft);
    }

    private void Deactivate()
    {
        var cti = _currentTextInput;
        _currentTextInput = null;
        cti?.Dispose();

        if (AttachedView.UpdateMode == TextUpdateMode.Unfocus)
        {
            AttachedView.Text?.SetText(_currentText);
        }
        else
        {
            _currentText = AttachedView.Text?.ToString() ?? "";
            _cursorPosition = _currentText?.Length ?? 0;
        }

        _isActiveInput = false;
    }

    public void SetFocus() => Focused = true;

    public bool ResetFocus()
    {
        Focused = false;
        Deactivate();
        return true;
    }

    public string UniqueId => AttachedView?.UniqueId;
    public bool SkipNavigation => false;

    protected IFont GetFont()
    {
        var size = AttachedView.Font?.FontSize ?? 12;

        if (AttachedView.Scaling == TextScaling.Default)
        {
            size = (int)MathF.Ceiling(size * CurrentScale);
        }

        var font = fontsManager.GetFont(AttachedView.Font?.FontFamily ?? "Default", size);

        _scale = (float)size / Math.Max(1, font.Size);
        return font;
    }

    public void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context)
    {
        _hovered = Enabled && hoverPosition.HasValue &&
                   (!matchingPointerId.HasValue || matchingPointerId.Value == _currentPointerId) &&
                   ScreenBounds.Contains(hoverPosition.Value);
    }

    public bool ProcessInput(IReadOnlyList<Pointer> pointers, IInputContext context)
    {
        if (_isActiveInput)
        {
            return ProcessActiveInput(pointers, context);
        }
        
        if(_wasActiveInput) return false;
        
        if (_currentPointerId.HasValue)
        {
            var pointer = pointers.FirstOrDefault(o => o.Id == _currentPointerId.Value);
            if (pointer is not null)
            {
                if (!pointer.State.IsDown)
                {
                    if (pointer.State.IsChanged && ScreenBounds.Contains(pointer.Position))
                    {
                        (uiSounds[UiSounds.ExecuteSound] ?? uiSounds[UiSounds.ButtonReleaseSound])?.PlayOnce();
                        Activate(pointer.Position);
                    }
                    _currentPointerId = null;
                    _pushed = false;
                }
                else
                {
                    var wasPressed = _pushed;
                    _pushed = ScreenBounds.Contains(pointer.Position);
                    if (wasPressed != _pushed)
                    {
                        if (_pushed) uiSounds[UiSounds.ButtonPushSound]?.PlayOnce();
                        else uiSounds[UiSounds.ButtonReleaseSound]?.PlayOnce();
                    }
                }
                return true;
            }
            _currentPointerId = null;
            _pushed = false;
        }

        foreach (var pointer in pointers)
        {
            if (pointer.State == ButtonState.JustPressed)
            {
                if (ScreenBounds.Contains(pointer.Position))
                {
                    _currentPointerId = pointer.Id;
                    _pushed = true;
                    uiSounds[UiSounds.ButtonPushSound]?.PlayOnce();

                    var focusable = context.FindFocusable(null, this);
                    if (focusable != null)
                    {
                        context.Focus(this, this);
                        pageInputContext.ShowFocus = false;
                    }
                    context.CapturePointer(pointer.Id, this);
                    return true;
                }

                Deactivate();
            }
        }

        return false;
    }

    private bool ProcessActiveInput(IReadOnlyList<Pointer> pointers, IInputContext context)
    {
        if (_currentPointerId != null)
        {
            var pointer = pointers.FirstOrDefault(o => o.Id == _currentPointerId.Value);
            if (pointer is not null)
            {
                if (!pointer.State.IsDown)
                {
                    _currentPointerId = null;
                    return true;
                }
                
                if (ScreenBounds.Contains(pointer.Position))
                {
                    Activate(pointer.Position);
                }
            }

            return true;
        }

        var ptr =
            pointers.FirstOrDefault(o => o.State == ButtonState.JustPressed && ScreenBounds.Contains(o.Position));

        if (ptr != null)
        {
            context.CapturePointer(ptr.Id, this);
            _currentPointerId = ptr.Id;
            Activate(ptr.Position);
            return true;
        }

        if (pointers.Any(o => o.State == ButtonState.JustPressed))
        {
            Deactivate();
        }
        return false;
    }

    public void CancelPointer(int pointerId, IInputContext context)
    {
        if (_currentPointerId == pointerId || context.FindFocusable(null, this) != this)
        {
            _currentPointerId = null;
            _pushed = false;
        }
    }

    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer);

        _wasActiveInput = _isActiveInput;
        
        var bgColor = GetColor(AttachedView.BackgroundColors, renderer);

        var sb = ScreenBounds;
        if (bgColor?.A > 0)
        {
            renderer.GeometryRenderer.FillRectangle(sb, renderer.CurrentPass == RenderPass.Glow ? RgbaColor.Black : bgColor.Value);
        }

        var frameColor = GetColor(AttachedView.FrameColors, renderer);

        if (frameColor?.A > 0)
        {
            renderer.GeometryRenderer.DrawFrame(sb, renderer.CurrentPass == RenderPass.Glow ? RgbaColor.Black : frameColor.Value, AttachedView.FrameThickness?.Calculate(CurrentScale, 1) ?? CurrentScale);
        }

        if (_isActiveInput)
        {
            frameColor = AttachedView.ActiveFrameColor?.GetColor(renderer);

            if (frameColor?.A > 0)
            {
                var thickness = AttachedView.ActiveFrameThickness?.Calculate(CurrentScale, 1) ?? CurrentScale;
                var frame = sb.Inflate(thickness + CurrentScale);

                renderer.GeometryRenderer.DrawFrame(frame, renderer.CurrentPass == RenderPass.Glow ? RgbaColor.Black : frameColor.Value, thickness);
            }
        }

        var font = GetFont();
        var textRect = GetTextRectangle();
        var isPlaceholder = string.IsNullOrEmpty(_currentText);

        float cursorPositionX = 0;

        renderer.StateManager.SaveState();
        renderer.StateManager.SetClipRect(textRect);

        var fullTextSize = font.TextSize(_currentText).Width * TextScale;
        
        if (_isActiveInput && !isPlaceholder)
        {
            cursorPositionX = font.TextSize(new TextSource(_currentText, 0, _cursorPosition)).Width * TextScale;
            
            var displayPosition = cursorPositionX + _textDisplayOffset;
            
            if (displayPosition > textRect.Width)
            {
                _textDisplayOffset -= displayPosition - textRect.Width;
            }

            if (displayPosition < 0)
            {
                _textDisplayOffset += -displayPosition;
            }
            
            var rightPosition = _textDisplayOffset + fullTextSize;
            if (rightPosition < textRect.Width)
            {
                _textDisplayOffset += textRect.Width - rightPosition;
                _textDisplayOffset = Math.Min(0, _textDisplayOffset);
            }
            
            renderer.StateManager.Translate(new Vector2(_textDisplayOffset, 0));
        }
        else
        {
            _textDisplayOffset = 0;
        }

        
        if (isPlaceholder)
        {
            if (AttachedView.Placeholder is not null)
            {
                var placeholderColor = GetColor(AttachedView.PlaceholderColors, renderer);
                var placeholderOutlineColor = GetColor(AttachedView.PlaceholderOutlineColors, renderer);
                renderer.TextRenderer.DrawText(
                    font: font,
                    text: AttachedView.Placeholder,
                    position: textRect,
                    align: ContentAlign.Left | ContentAlign.VCenter,
                    scale: TextScale,
                    color: placeholderColor,
                    outlineColor: placeholderOutlineColor,
                    context: _textRenderingContext);
            }
        }
        else
        {
            if (_selectionStart >= 0)
            {
                var selectionColor = AttachedView.SelectionColor?.GetColor(renderer) ?? RgbaColor.White;
                
                var min = Math.Min(_selectionStart, _cursorPosition);
                var max = Math.Max(_selectionStart, _cursorPosition);
                
                var start = font.TextSize(new TextSource(_currentText, 0, min)).Width * TextScale;
                var end = font.TextSize(new TextSource(_currentText, 0, max)).Width * TextScale;

                var rect = new RectangleF(textRect.X + start, textRect.Y, end - start, font.LineSize * TextScale);
                renderer.GeometryRenderer.FillRectangle(rect, selectionColor);
            }
            
            var textColor = GetColor(AttachedView.TextColors, renderer);
            var textOutlineColor = GetColor(AttachedView.TextOutlineColors, renderer);
            renderer.TextRenderer.DrawText(
                font: font,
                text: _currentText,
                position: textRect.SetWidth(fullTextSize + 10),
                align: ContentAlign.Left | ContentAlign.VCenter,
                scale: TextScale,
                color: textColor,
                outlineColor: textOutlineColor,
                context: _textRenderingContext);
        }
        
        if (_isActiveInput)
        {
            if (DateTime.Now.TimeOfDay.TotalSeconds % 1 < 0.5f)
            {
                var textOutlineColor = GetColor(AttachedView.TextOutlineColors, renderer);
                frameColor = AttachedView.CursorColor?.GetColor(renderer) ?? frameColor;
                
                var cursorRect = new RectangleF(textRect.X + cursorPositionX - 0.5f, textRect.Y, TextScale,
                    font.LineSize * TextScale);

                renderer.GeometryRenderer.FillRectangle(cursorRect, frameColor ?? RgbaColor.White);

                cursorRect = cursorRect.Inflate(TextScale);

                renderer.GeometryRenderer.DrawFrame(cursorRect, textOutlineColor ?? RgbaColor.White, TextScale);
            }
        }
        
        renderer.StateManager.RestoreState();
    }

    private RectangleF GetTextRectangle()
    {
        var xx = ScreenBounds.X;
        var yy = ScreenBounds.Y;
        var width = ScreenBounds.Width;
        var height = ScreenBounds.Height;

        if (AttachedView.Padding?.Left.HasValue ?? false)
        {
            var pad = AttachedView.Padding.Value.Left.Value.Calculate(CurrentScale, ScreenBounds.Width);
            width -= pad;
            xx += pad;
        }
        if (AttachedView.Padding?.Right.HasValue ?? false)
        {
            width -= AttachedView.Padding.Value.Right.Value.Calculate(CurrentScale, ScreenBounds.Width);
        }
        if (AttachedView.Padding?.Top.HasValue ?? false)
        {
            var pad = AttachedView.Padding.Value.Top.Value.Calculate(CurrentScale, ScreenBounds.Height);
            height -= pad;
            yy += pad;
        }
        if (AttachedView.Padding?.Bottom.HasValue ?? false)
        {
            height -= AttachedView.Padding.Value.Bottom.Value.Calculate(CurrentScale, ScreenBounds.Height);
        }

        return new RectangleF(xx, yy, width, height);
    }

    private RgbaColor? GetColor(IButtonStateColors colors, IRenderer renderer) => colors?.GetColor(renderer, _hovered, Focused, _pushed, Enabled, _isActiveInput);

    public void OnTextInput(string text)
    {
        DeleteSelectionText();
        
        var beforeCursor = _currentText.Substring(0, _cursorPosition);
        var afterCursor = _currentText.Substring(_cursorPosition);
        
        _currentText = beforeCursor + text + afterCursor;
        _cursorPosition += text.Length;
        
        if (AttachedView.UpdateMode == TextUpdateMode.Live)
        {
            AttachedView.Text?.SetText(_currentText);
        }
    }

    public void OnTextInputClosed() => Deactivate();
    
    public bool OnKey(Key key)
    {
        switch(key)
        {
            case Key.Enter:
                if (_isActiveInput)
                {
                    AttachedView.Text?.SetText(_currentText);
                    Deactivate();
                    return true;
                }
                break;
            
            case Key.Escape:
                if (_isActiveInput)
                {
                    Deactivate();
                    return true;
                }
                break;
            case Key.Backspace:
                
                if (!DeleteSelectionText())
                {
                    if (_cursorPosition > 0)
                    {
                        _currentText = _currentText.Substring(0, _cursorPosition - 1) +
                                       _currentText.Substring(_cursorPosition);
                        _selectionStart = -1;
                        _cursorPosition--;
                    }
                }

                if (AttachedView.UpdateMode == TextUpdateMode.Live)
                {
                    AttachedView.Text?.SetText(_currentText);
                }
                return true;
            
            case Key.Delete:
                if (!DeleteSelectionText() && _currentText.Length > _cursorPosition)
                {
                    _currentText = _currentText.Substring(0, _cursorPosition) +
                                   _currentText.Substring(_cursorPosition + 1);
                    
                    _selectionStart = -1;
                }
                if (AttachedView.UpdateMode == TextUpdateMode.Live)
                {
                    AttachedView.Text?.SetText(_currentText);
                }
                return true;
            
            case Key.Left:
                SetCursorPosition(_cursorPosition - 1);
                return true;
            
            case Key.Right:
                SetCursorPosition(_cursorPosition + 1);
                return true;
            
            case Key.Home:
                SetCursorPosition(0);
                return true;
            
            case Key.End:
                SetCursorPosition(int.MaxValue);
                return true;
        }
        return false;
    }

    private void SetCursorPosition(int position)
    {
        if (_currentTextInput.IsShiftPressed)
        {
            if (_selectionStart < 0)
            {
                _selectionStart = _cursorPosition;
            }
        }
        else
        {
            _selectionStart = -1;
        }
        
        _cursorPosition = Math.Min(_currentText.Length, Math.Max(position, 0));
    }

    private bool DeleteSelectionText()
    {
        if (_selectionStart < 0) return false;
        
        var min = Math.Min(_selectionStart, _cursorPosition);
        var max = Math.Max(_selectionStart, _cursorPosition);
        
        _currentText = _currentText.Substring(0, min) + _currentText.Substring(max);
        
        _cursorPosition = min;
        _selectionStart = -1;
        return true;
    }
}
