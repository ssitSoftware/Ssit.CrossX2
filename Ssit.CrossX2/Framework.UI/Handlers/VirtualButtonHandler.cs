using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class VirtualButtonHandler(ViewHandler.CreateHandlerParameters parameters, IHandlerMapper handlerMapper, IVirtualGameInput virtualGameInput,
    IHapticDevice hapticDevice, IRenderer renderer) 
    : ContainerHandler<VirtualButton>(parameters, handlerMapper), IInputConsumer, IColorSource
{
    public RgbaColor? OutlineColor => null;
    
    private int? _currentPointerId;
    
    public void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context)
    {
    }

    public bool ProcessInput(IReadOnlyList<Pointer> pointers, IInputContext context)
    {
        if (_currentPointerId.HasValue)
        {
            var pointer = pointers.FirstOrDefault(o => o.Id == _currentPointerId.Value);
            if (pointer is not null)
            {
                virtualGameInput.SetButton(AttachedView.Button, pointer.State);
                
                if (pointer.State == ButtonState.Empty)
                {
                    _currentPointerId = null;
                }
                
                if (pointer.State == ButtonState.JustReleased && true == AttachedView.HapticFeedback?.Value)
                {
                    hapticDevice.Feedback(FeedbackStyle.ButtonRelease, pointer.OriginalPosition);
                }
                return false;
            }
            
            _currentPointerId = null;
            virtualGameInput.SetButton(AttachedView.Button, ButtonState.Empty);
        }
        
        foreach (var pointer in pointers)
        {
            if (pointer.State == ButtonState.JustPressed)
            {
                if (ScreenBounds.Contains(pointer.Position))
                {
                    _currentPointerId = pointer.Id;
                    context.CapturePointer(pointer.Id, this);
                    
                    virtualGameInput.SetButton(AttachedView.Button, ButtonState.JustPressed);
                    context.CapturePointer(pointer.Id, this);

                    if (true == AttachedView.HapticFeedback?.Value)
                    {
                        hapticDevice.Feedback(FeedbackStyle.ButtonPush, pointer.OriginalPosition);
                    }

                    return true;
                }
            }
        }

        return false;
    }

    public void CancelPointer(int pointerId, IInputContext context)
    {
        if (_currentPointerId == pointerId)
        {
            _currentPointerId = null;
            virtualGameInput.SetButton(AttachedView.Button, ButtonState.Empty);
        }
    }

    protected override void OnDispose(bool disposing)
    {
        virtualGameInput.SetButton(AttachedView.Button, ButtonState.Empty);
        base.OnDispose(disposing);
    }

    public RgbaColor? GetColor(string id)
    {
        switch (id)
        {
            case nameof(VirtualButton.ColorPressed):
                return _currentPointerId.HasValue ? AttachedView.ColorPressed.GetColor(renderer) : null;
            
            case nameof(VirtualButton.OutlineColorPressed):
                return _currentPointerId.HasValue ? AttachedView.OutlineColorPressed.GetColor(renderer) : null;
        }

        return null;
    }
}