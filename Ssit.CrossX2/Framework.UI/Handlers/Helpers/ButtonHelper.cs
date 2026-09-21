using System.Numerics;
using Ssit.CrossX2.Framework.Commands;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Common.Pages;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers.Helpers;

public class ButtonHelper<TView, TViewHandler>: IDisposable where TView: View, IButtonView where TViewHandler: ViewHandler<TView>, IFocusable, IInputConsumer
{
    public bool IsEnabled
    {
        get => field || _isExecutingDelayedCommand;
        private set;
    }
    
    public bool IsHovered { get; private set; }
    public bool IsPressed { get; private set; }
    
    public bool IsExecutingCommand => _isExecutingDelayedCommand;

    private readonly TViewHandler _viewHandler;
    private readonly IUiSounds _uiSounds;
    private readonly IHapticDevice _hapticDevice;
    private readonly PageInputContext _pageInputContext;

    private TView AttachedView => (TView)_viewHandler.View;
    
    private int? _currentPointerId;
    private bool _isExecutingDelayedCommand;

    private bool HapticEnabled => (_viewHandler?.View as IButtonView)?.HapticFeedback?.Value ?? false;
    
    public ButtonHelper(TViewHandler viewHandler, IUiSounds uiSounds, IHapticDevice hapticDevice, PageInputContext pageInputContext)
    {
        _viewHandler = viewHandler;
        _uiSounds = uiSounds;
        _hapticDevice = hapticDevice;
        _pageInputContext = pageInputContext;

        if (AttachedView.Command is not null)
        {
            AttachedView.Command.CanExecuteChanged += CommandOnCanExecuteChanged;
            IsEnabled = AttachedView.Command.CanExecute(AttachedView.CommandParameter);
        }
    }
    
    private void CommandOnCanExecuteChanged(object sender, EventArgs e)
    {
        IsEnabled = AttachedView.Command.CanExecute(AttachedView.CommandParameter);
    }
    
    public void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context)
    {
        if (_isExecutingDelayedCommand)
        {
            return;
        }
        
        IsHovered = !_isExecutingDelayedCommand && IsEnabled && hoverPosition.HasValue && 
                    (!matchingPointerId.HasValue || matchingPointerId.Value == _currentPointerId)  &&
                    _viewHandler.ScreenBounds.Contains(hoverPosition.Value);
    }

    public bool ProcessInput(IReadOnlyList<Pointer> pointers, IInputContext context)
    {
        if (_isExecutingDelayedCommand || !AttachedView.EnabledCommandTypes.HasFlag(ButtonCommandType.Select))
        {
            return true;
        }

        if ( _currentPointerId.HasValue )
        {
            var pointer = pointers.FirstOrDefault(o => o.Id == _currentPointerId.Value);
            if (pointer is not null)
            {
                if (!pointer.State.IsDown)
                {
                    if (pointer.State.IsChanged)
                    {
                        if (_viewHandler.ScreenBounds.Contains(pointer.Position))
                        {
                            if (HapticEnabled)
                            {
                                _hapticDevice.Feedback(FeedbackStyle.ButtonRelease, pointer.OriginalPosition);
                            }
                            
                            Execute(TimeSpan.Zero, AttachedView.EnabledCommandTypes > ButtonCommandType.Select ? ButtonCommandType.Select : null);
                            _currentPointerId = null;
                            return true;
                        }
                    }
                    _currentPointerId = null;
                    IsPressed = false;
                }
                else
                {
                    var wasPressed = IsPressed;
                    IsPressed = _viewHandler.ScreenBounds.Contains(pointer.Position);

                    if (wasPressed != IsPressed)
                    {
                        if (IsPressed)
                        {
                            if (HapticEnabled)
                            {
                                _hapticDevice.Feedback(FeedbackStyle.ButtonPush, pointer.OriginalPosition);
                            }

                            _uiSounds[UiSounds.ButtonPushSound]?.PlayOnce();
                        }
                        else
                        {
                            if (HapticEnabled)
                            {
                                _hapticDevice.Feedback(FeedbackStyle.ButtonRelease, pointer.OriginalPosition);
                            }

                            _uiSounds[UiSounds.ButtonReleaseSound]?.PlayOnce();
                        }
                    }
                }
                return true;
            }
            _currentPointerId = null;
            IsPressed = false;
        }
        
        foreach (var pointer in pointers)
        {
            if (pointer.State == ButtonState.JustPressed)
            {
                if (_viewHandler.ScreenBounds.Contains(pointer.Position))
                {
                    _currentPointerId = pointer.Id;
                    IsPressed = true;

                    if (HapticEnabled)
                    {
                        _hapticDevice.Feedback(FeedbackStyle.ButtonPush, pointer.OriginalPosition);
                    }

                    _uiSounds[UiSounds.ButtonPushSound]?.PlayOnce();
                    
                    var focusable = context.FindFocusable(null, _viewHandler);
                    if (focusable != null)
                    {
                        context.Focus(_viewHandler, _viewHandler);
                        _pageInputContext.ShowFocus = false;
                    }
                    context.CapturePointer(pointer.Id, _viewHandler);
                    return true;
                }
            }
        }
        
        return false;
    }

    public void CancelPointer(int pointerId, IInputContext context)
    {
        if (_currentPointerId == pointerId || context.FindFocusable(null, _viewHandler) != _viewHandler)
        {
            _currentPointerId = null;
            IsPressed = false;
        }
    }

    public bool OnUiButton(UiButton button, IInputContext context)
    {
        if (_isExecutingDelayedCommand)
        {
            return true;
        }

        FocusDirection focusDirection = FocusDirection.None;
        
        switch (button)
        {
            case UiButton.Left:
                if (AttachedView.EnabledCommandTypes.HasFlag(ButtonCommandType.Previous))
                {
                    _uiSounds[UiSounds.ChangeValueSound]?.PlayOnce();
                    Execute(TimeSpan.Zero, ButtonCommandType.Previous);
                }
                else focusDirection = FocusDirection.Left;
                break;
            
            case UiButton.Right:
                if (AttachedView.EnabledCommandTypes.HasFlag(ButtonCommandType.Next) )
                {
                    _uiSounds[UiSounds.ChangeValueSound]?.PlayOnce();
                    Execute(TimeSpan.Zero, ButtonCommandType.Next);
                }
                else focusDirection = FocusDirection.Right;
                break;
            
            case UiButton.Up:
                focusDirection = FocusDirection.Up;
                break;
            
            case UiButton.Down:
                focusDirection = FocusDirection.Down;
                break;
            
            case UiButton.Select:
                if (IsEnabled)
                {
                    if (AttachedView.EnabledCommandTypes.HasFlag(ButtonCommandType.Select))
                    {
                        if (!_pageInputContext.ShowFocus)
                        {
                            _uiSounds[UiSounds.ItemNavigateSound]?.PlayOnce();
                            _pageInputContext.ShowFocus = true;
                            return false;
                        }

                        if (AttachedView!.KeyCommandDelay.Milliseconds > 10)
                        {
                            _uiSounds[UiSounds.ButtonPushSound]?.PlayOnce();
                        }

                        Execute(AttachedView.KeyCommandDelay,
                            AttachedView.EnabledCommandTypes > ButtonCommandType.Select ? ButtonCommandType.Select : null);
                    }
                }
                break;
        }

        if (focusDirection != FocusDirection.None)
        {
            if (!_pageInputContext.ShowFocus)
            {
                _uiSounds[UiSounds.ItemNavigateSound]?.PlayOnce();
                _pageInputContext.ShowFocus = true;
                context.Focus(_viewHandler, _viewHandler);
                return true;
            }

            if (context.MoveFocus(focusDirection, _viewHandler))
            {
                _uiSounds[UiSounds.ItemNavigateSound]?.PlayOnce();
            }
        }

        return false;
    }

    private void Execute(TimeSpan delay, ButtonCommandType? commandType)
    {
        if (commandType.HasValue)
        {
            if (!(AttachedView.Command?.CanExecute((AttachedView.CommandParameter, commandType.Value)) ?? false))
                return;
        }
        else
        {
            if (!(AttachedView.Command?.CanExecute(AttachedView.CommandParameter) ?? false))
                return;
        }
        
        if (AttachedView.Command is not IAsyncCommand asyncCommand)
        {
            asyncCommand = new AsyncCommand( o => Task.Run( async () =>
            {
                await Task.Yield();
                AttachedView.Command?.Execute(o);
            }));
        }
        
        IsPressed = true;
        _isExecutingDelayedCommand = true;

        Task.Run(async () =>
        {
            if (delay.TotalSeconds > 0)
            {
                await Task.Delay(delay);
            }

            if (!commandType.HasValue || commandType == ButtonCommandType.Select)
            {
                (_uiSounds[UiSounds.ExecuteSound] ?? _uiSounds[UiSounds.ButtonReleaseSound])?.PlayOnce();
            }

            IsPressed = false;
            if (AttachedView.CommandDelay.TotalSeconds > 0)
            {
                await Task.Delay(AttachedView.CommandDelay);
            }

            if (commandType.HasValue)
            {
                await asyncCommand.ExecuteAsync((AttachedView.CommandParameter, commandType.Value));
            }
            else
            {
                await asyncCommand.ExecuteAsync(AttachedView.CommandParameter);
            }

        }).ContinueWith(_ =>
        {
            IsPressed = false;
            _isExecutingDelayedCommand = false;
        });
    }

    public void Dispose()
    {
        if (AttachedView.Command is not null)
        {
            AttachedView.Command.CanExecuteChanged -= CommandOnCanExecuteChanged;
        }
    }
}