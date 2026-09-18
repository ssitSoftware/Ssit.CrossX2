#if IOS

using CoreFoundation;
using Ssit.CrossX2.Input;

namespace Ssit.CrossX2._Sdl3Impl.Input;

internal class NativeTextInputServiceIos : INativeTextInputService
{
    private NativeInputView _inputView;
    private NativeTextInputIos _current;
    private INativeTextInputConsumer _currentConsumer;
    private RectangleF? _newBounds;
    private float _keyboardTop = float.MaxValue; // MaxValue = keyboard not visible
    private CGAffineTransform? _originalTransform;
    private bool _isShiftPressed;

    internal bool IsShiftPressed => _isShiftPressed;

    public INativeTextInput AllocateTextInput(INativeTextInputConsumer consumer, InputType inputType)
    {
        _current?.Dispose();
        _currentConsumer = consumer;
        var input = new NativeTextInputIos(this);
        _current = input;

        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            if (_inputView == null)
            {
                _inputView = new NativeInputView(this);
                GetKeyWindow()?.AddSubview(_inputView);

                NSNotificationCenter.DefaultCenter.AddObserver(
                    UIKeyboard.WillShowNotification, OnKeyboardWillShow);
                NSNotificationCenter.DefaultCenter.AddObserver(
                    UIKeyboard.WillHideNotification, OnKeyboardWillHide);
            }

            _inputView.BecomeFirstResponder();
        });

        return input;
    }

    internal void OnDisposed(NativeTextInputIos input)
    {
        if (_current != input) return;
        _current = null;
        var consumer = _currentConsumer;
        _currentConsumer = null;
        
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            _inputView?.ResignFirstResponder();
        });

        consumer?.OnTextInputClosed();
    }

    internal void UpdatePosition(RectangleF bounds, int _)
    {
        _newBounds = bounds;
        DispatchQueue.MainQueue.DispatchAsync(RecalculateAndApplyOffset);
    }

    private static float PixelsToPoints(float pixels) =>
        (float)(pixels / UIScreen.MainScreen.Scale);
    

    private void RecalculateAndApplyOffset()
    {
        var bounds = _newBounds;
        if (bounds == null || _keyboardTop == float.MaxValue) return;

        var inputBottom = PixelsToPoints(bounds.Value.Bottom);
        var overlap = inputBottom - _keyboardTop;
        
        var targetOffset = (float)(overlap > 0 ? overlap + 8 : 0);
        ShiftRootView(targetOffset);
    }

    private void OnKeyboardWillShow(NSNotification notification)
    {
        var keyboardFrame = UIKeyboard.FrameEndFromNotification(notification);
        _keyboardTop = (float)(UIScreen.MainScreen.Bounds.Height - keyboardFrame.Height);

        var duration = UIKeyboard.AnimationDurationFromNotification(notification);
        UIView.Animate(duration, RecalculateAndApplyOffset);
    }

    private void OnKeyboardWillHide(NSNotification notification)
    {
        _keyboardTop = float.MaxValue;
        var duration = UIKeyboard.AnimationDurationFromNotification(notification);
        UIView.Animate(duration, () =>
        {
            ShiftRootView(0);
            _originalTransform = null;
        });
    }

    private void ShiftRootView(float offset)
    {
        var window = GetKeyWindow();
        if (window == null) return;
        _originalTransform ??= window.Transform;
        var t = _originalTransform.Value;
        t.Ty -= offset;
        window.Transform = t;
    }

    private static UIWindow GetKeyWindow() =>
        UIApplication.SharedApplication.Windows.FirstOrDefault(w => w.IsKeyWindow);

    internal void OnText(string text) => _currentConsumer?.OnTextInput(text);
    internal void OnBackspace() => _currentConsumer?.OnKey(Key.Backspace);
    internal void OnEnter() => _currentConsumer?.OnKey(Key.Enter);
    internal void OnKey(Key key) => _currentConsumer?.OnKey(key);

    private class NativeInputView : UIView, IUIKeyInput
    {
        private readonly NativeTextInputServiceIos _service;

        public NativeInputView(NativeTextInputServiceIos service)
        {
            _service = service;
            Frame = new CGRect(0, 0, 1, 1);
        }

        public override bool CanBecomeFirstResponder => true;

        // Always return true so the keyboard doesn't think there's nothing to delete
        public bool HasText => true;

        public void InsertText(string text)
        {
            if (text == "\n")
                _service.OnEnter();
            else
                _service.OnText(text);
        }

        public void DeleteBackward() => _service.OnBackspace();

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            var handled = false;
            foreach (UIPress press in presses)
            {
                _service._isShiftPressed = (press.Key?.ModifierFlags & UIKeyModifierFlags.Shift) != 0;

                switch (press.Key?.KeyCode)
                {
                    case UIKeyboardHidUsage.KeyboardLeftArrow:
                        _service.OnKey(Key.Left);
                        handled = true;
                        break;
                    case UIKeyboardHidUsage.KeyboardRightArrow:
                        _service.OnKey(Key.Right);
                        handled = true;
                        break;
                    
                    case  UIKeyboardHidUsage.KeyboardHome:
                        _service.OnKey(Key.Home);
                        handled = true;
                        break;
                    
                    case  UIKeyboardHidUsage.KeyboardEnd:
                        _service.OnKey(Key.End);
                        handled = true;
                        break;
                    
                    case UIKeyboardHidUsage.KeyboardDeleteForward:
                        _service.OnKey(Key.Delete);
                        handled = true;
                        break;
                }
            }

            if (!handled)
            {
                base.PressesBegan(presses, evt);
            }
        }

        public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            foreach (UIPress press in presses)
            {
                var code = press.Key?.KeyCode;
                if (code == UIKeyboardHidUsage.KeyboardLeftShift || code == UIKeyboardHidUsage.KeyboardRightShift)
                    _service._isShiftPressed = false;
            }
            base.PressesEnded(presses, evt);
        }
    }
}

#endif
