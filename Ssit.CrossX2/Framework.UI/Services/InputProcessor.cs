using System.Diagnostics;
using System.Numerics;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Services;

internal sealed class InputProcessor: IInputContext
{
    private readonly IKeyboard _keyboard;
    private readonly IGameControllers _gameControllers;
    private readonly IPointingDevices _pointingDevices;
    private readonly Navigation _navigation;
    private readonly IVirtualGameInput _virtualGameInput;
    private readonly IInputCoordinateSystem _coordinateSystem;

    private readonly List<IInputConsumer> _inputConsumers = new();
    private readonly List<(int, IInputConsumer)> _capturedPointers = new();

    private readonly List<Pointer> _processingPointers = new();
    
    private readonly DateTime[] _lastDirectionExecute = new DateTime[4];
    
    private const int Left = 0;
    private const int Right = 1;
    private const int Up = 2;
    private const int Down = 3;
    
    
    public InputProcessor(IKeyboard keyboard, IGameControllers gameControllers, IPointingDevices pointingDevices, 
        Navigation navigation, IVirtualGameInput virtualGameInput, IInputCoordinateSystem coordinateSystem = null)
    {
        _keyboard = keyboard;
        _gameControllers = gameControllers;
        _pointingDevices = pointingDevices;
        _navigation = navigation;
        _virtualGameInput = virtualGameInput;
        _coordinateSystem = coordinateSystem;
    }

    private void PrepareInputConsumers(ViewHandler handler)
    {
        if (!handler.View.Visible.Value)
            return;
        
        if (handler is IInputConsumer consumer)
        {
            _inputConsumers.Add(consumer);
        }
        
        if (handler is IChildrenContainer container)
        {
            foreach (var child in container.Children)
            {
                PrepareInputConsumers(child);
            }
        }
    }

    private void OnUiButton(IPage page, UiButton button)
    {
        try
        {
            if (true == page.FocusedElement?.DisableAllInput)
                return;

            if (page.FocusedElement?.OnUiButton(button, this) ?? false)
            {
                return;
            }
            
            if (page.OnUiButton(button, this))
            {
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Debugger.Break();
        }
    }
    
    public void Process()
    {
        var page = _navigation.CurrentPage;
        if (page is null)
            return;
        
        if (Check(GetUiButtonLeft(), Left))
        {
            OnUiButton(page, UiButton.Left);
            if (page != _navigation.CurrentPage) return;
        }

        if (Check(GetUiButtonRight(), Right))
        {
            OnUiButton(page, UiButton.Right);
            if (page != _navigation.CurrentPage) return;
        }

        if (Check(GetUiButtonUp(), Up))
        {
            OnUiButton(page, UiButton.Up);
            if (page != _navigation.CurrentPage) return;
        }

        if (Check(GetUiButtonDown(), Down))
        {
            OnUiButton(page, UiButton.Down);
            if (page != _navigation.CurrentPage) return;
        }

        if (GetUiButtonSelect())
        {
            OnUiButton(page, UiButton.Select);
            if (page != _navigation.CurrentPage) return;
        }
        
        if (GetUiButtonMenu())
        {
            OnUiButton(page, UiButton.Menu);
            if (page != _navigation.CurrentPage) return;
        }

        if (GetUiButtonMenuOrBack())
        {
            OnUiButton(page, UiButton.MenuOrBack);
            if (page != _navigation.CurrentPage) return;
        }

        if (GetUiButtonBack())
        {
            OnUiButton(page, UiButton.Back);
            if (page != _navigation.CurrentPage) return;
        }
        
        _inputConsumers.Clear();
        PrepareInputConsumers(page.RootHandler);

        var transform = _coordinateSystem?.Transform ?? Matrix3x2.Identity;
        
        _pointingDevices.PushTransform(transform);

        try
        {
            int? matchingPointerId = null;
            foreach (var ptr in _pointingDevices.Pointers)
            {
                if (ptr.Position == _pointingDevices.HoverPosition)
                {
                    matchingPointerId = ptr.Id;
                    break;
                }
            }

            bool disableInput = true == page.FocusedElement?.DisableAllInput;

            if (disableInput)
            {
                for (var idx = 0; idx < _pointingDevices.Pointers.Count; ++idx)
                {
                    CapturePointer(_pointingDevices.Pointers[idx].Id, page.FocusedElement as IInputConsumer);
                }

                ProcessCapturedPointers();
            }
            else
            {
                for (var idx = _inputConsumers.Count - 1; idx >= 0; --idx)
                {
                    _inputConsumers[idx].ProcessHover(_pointingDevices.HoverPosition, matchingPointerId, this);
                }

                _processingPointers.Clear();
                _processingPointers.AddRange(_pointingDevices.Pointers);
                
                for (var idx = _inputConsumers.Count - 1; idx >= 0; --idx)
                {
                    if (_inputConsumers[idx].ProcessInput(_processingPointers, this))
                    {
                        ProcessCapturedPointers();
                        _inputConsumers[idx].ProcessHover(_pointingDevices.HoverPosition, matchingPointerId, this);
                    }
                }
            }
        }
        finally
        {
            _pointingDevices.PopTransform();
        }
    }

    private bool Check(ButtonState state, int index)
    {
        if (!state.IsDown) return false;

        var now = DateTime.Now;

        if (state.IsChanged)
        {
            _lastDirectionExecute[index] = now + TimeSpan.FromMilliseconds(150);
            return true;
        }

        if (now - _lastDirectionExecute[index] > TimeSpan.FromMilliseconds(150))
        {
            _lastDirectionExecute[index] = now;
            return true;
        }
        
        return false;
    }

    private void ProcessCapturedPointers()
    {
        if (_capturedPointers.Count > 0)
        {
            foreach (var (pointerId, consumer) in _capturedPointers)
            {
                foreach (var cons in _inputConsumers)
                {
                    if (cons != consumer)
                    {
                        cons.CancelPointer(pointerId, this);
                    }
                }
                
                _processingPointers.RemoveAll(o => o.Id == pointerId);
            }
            _capturedPointers.Clear();
        }
    }
    
    private bool GetUiButtonSelect()
    {
        return _keyboard.GetKey(Key.Return) == ButtonState.JustPressed ||
               _gameControllers.GetButton(0, GameControllerButton.A) == ButtonState.JustPressed ||
               _virtualGameInput.GetButton(GameControllerButton.A) == ButtonState.JustPressed;
    }
    
    private bool GetUiButtonBack()
    {
        return _gameControllers.GetButton(0, GameControllerButton.B) == ButtonState.JustPressed ||
               _virtualGameInput.GetButton(GameControllerButton.B) == ButtonState.JustPressed;
    }
    
    private bool GetUiButtonMenu()
    {
        return _gameControllers.GetButton(0, GameControllerButton.Start) == ButtonState.JustPressed ||
               _virtualGameInput.GetButton(GameControllerButton.Start) == ButtonState.JustPressed;
    }

    private bool GetUiButtonMenuOrBack()
    {
        return _keyboard.GetKey(Key.Escape) == ButtonState.JustPressed;
    }
    
    private ButtonState GetUiButtonLeft()
    {
        return _keyboard.GetKey(Key.Left) |
               _gameControllers.GetButton(0, GameControllerButton.DPadLeft) |
               _gameControllers.GetButton(0, GameControllerButton.LeftStickLeft) |
               _virtualGameInput.GetButton(GameControllerButton.DPadLeft);
    }
    
    private ButtonState GetUiButtonRight()
    {
        return _keyboard.GetKey(Key.Right) |
               _gameControllers.GetButton(0, GameControllerButton.DPadRight) |
               _gameControllers.GetButton(0, GameControllerButton.LeftStickRight) |
               _virtualGameInput.GetButton(GameControllerButton.DPadRight);

    }
    
    private ButtonState GetUiButtonUp()
    {
        return _keyboard.GetKey(Key.Up) |
               _gameControllers.GetButton(0, GameControllerButton.DPadUp) |
               _gameControllers.GetButton(0, GameControllerButton.LeftStickUp) |
               _virtualGameInput.GetButton(GameControllerButton.DPadUp);
    }
    
    private ButtonState GetUiButtonDown()
    {
        return _keyboard.GetKey(Key.Down) |
               _gameControllers.GetButton(0, GameControllerButton.DPadDown) |
               _gameControllers.GetButton(0, GameControllerButton.LeftStickDown) |
               _virtualGameInput.GetButton(GameControllerButton.DPadDown);

    }

    public void CapturePointer(int pointerId, IInputConsumer captureBy)
    {
        _capturedPointers.Add((pointerId, captureBy));
    }

    public bool Focus(IFocusable focusable, object caller)
    {
        var page = GetPage(caller);
        if (false == page?.FocusedElement?.ResetFocus())
        {
            return false;
        }
        
        if (page != null)
        {
            page.FocusedElement = focusable;
        }
        return true;
    }

    private IPage GetPage(object caller)
    {
        if ( caller is IPage page)
        {
            return page;
        }

        if (caller is ViewHandler vh)
        {
            caller = vh.Parent;
        }
        
        if (caller is IViewParent parent)
        {
            while (parent is not null)
            {
                if (parent is IPage page1)
                {
                    return page1;
                }
                
                if (parent is ViewHandler handler)
                {
                    parent = handler.Parent;
                }
                else throw new InvalidProgramException();
            }
        }
        throw new InvalidProgramException();
    }

    public IFocusable FindFocusable(string uniqueId, object caller)
    {
        var page = GetPage(caller);

        if (uniqueId == null) return page.FocusedElement;
        return FindFocusable(page.RootHandler, uniqueId);
    }

    public bool MoveFocus(FocusDirection direction, object caller)
    {
        var page = GetPage(caller);
        return page.MoveFocus(direction);
    }

    public bool IsUiButtonDown(UiButton button)
    {
        switch (button)
        {
            case UiButton.Left:
                return _keyboard.GetKey(Key.Left).IsDown ||
                       _gameControllers.GetButton(0, GameControllerButton.DPadLeft).IsDown ||
                       _gameControllers.GetButton(0, GameControllerButton.LeftStickLeft).IsDown ||
                       _virtualGameInput.GetButton(GameControllerButton.DPadLeft).IsDown;
            
            case UiButton.Right:
                return _keyboard.GetKey(Key.Right).IsDown ||
                       _gameControllers.GetButton(0, GameControllerButton.DPadRight).IsDown ||
                       _gameControllers.GetButton(0, GameControllerButton.LeftStickRight).IsDown ||
                       _virtualGameInput.GetButton(GameControllerButton.DPadRight).IsDown;
            
            case UiButton.Up:
                return _keyboard.GetKey(Key.Up).IsDown ||
                       _gameControllers.GetButton(0, GameControllerButton.DPadUp).IsDown ||
                       _gameControllers.GetButton(0, GameControllerButton.LeftStickUp).IsDown ||
                       _virtualGameInput.GetButton(GameControllerButton.DPadUp).IsDown;
            
            case UiButton.Down:
                return _keyboard.GetKey(Key.Down).IsDown ||
                       _gameControllers.GetButton(0, GameControllerButton.DPadDown).IsDown ||
                       _gameControllers.GetButton(0, GameControllerButton.LeftStickDown).IsDown ||
                       _virtualGameInput.GetButton(GameControllerButton.DPadDown).IsDown;
        }

        return false;
    }

    private IFocusable FindFocusable(ViewHandler handler, string name)
    {
        if (handler is IFocusable focusable && focusable.UniqueId == name)
        {
            return focusable;
        }
        
        if (handler is IChildrenContainer container)
        {
            foreach (var child in container.Children ?? [])
            {
                var ret = FindFocusable(child, name);
                if(ret != null) return ret;
            }
        }

        return null;
    }
}