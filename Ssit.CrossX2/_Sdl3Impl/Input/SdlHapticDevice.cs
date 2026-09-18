#if !IOS
using System.Numerics;
using SDL;
using Ssit.CrossX2.Input;
using static SDL.SDL3;

namespace Ssit.CrossX2._Sdl3Impl.Input;

public class SdlHapticDevice: IHapticDevice
{
    public FeedbackLevel UiFeedbackLevel { get; set; } = FeedbackLevel.Level1;
    public FeedbackLevel ForceFeedbackLevel { get; set; } = FeedbackLevel.Level2;
    
    readonly SdlHandle<SDL_Haptic> _haptic;

    public unsafe SdlHapticDevice()
    {
        using var array = SDL_GetHaptics();
        foreach(var id in array!)
        {
            var hapticPtr = SDL_OpenHaptic(id);
            
            if (hapticPtr == null)
                continue;
            
            if (SDL_InitHapticRumble(hapticPtr) == true)
            {
                _haptic = new SdlHandle<SDL_Haptic>(hapticPtr);
                break;
            }
            SDL_CloseHaptic(hapticPtr);
        }
    }

    
    public void Feedback(FeedbackStyle style, Vector2? _) => Rumble(style);

    private unsafe void Rumble(FeedbackStyle style)
    {
        if (_haptic == null || _haptic.Pointer == null)
            return;

        uint timeInMs = 100;
        float strength = 1;
        
        switch (style)
        {
            case FeedbackStyle.ButtonPush:
                timeInMs = 30;
                strength = 0.1f * (int)UiFeedbackLevel;
                break;
            
            case FeedbackStyle.ButtonRelease:
                timeInMs = 30;
                strength = 0.05f * (int)UiFeedbackLevel;
                break;
            
            case FeedbackStyle.ControlChangeValue:
                timeInMs = 50;
                strength = 0.05f * (int)UiFeedbackLevel;
                break;
            
            case FeedbackStyle.RumbleLight:
                timeInMs = 100;
                strength = 0.1f * (int)ForceFeedbackLevel;
                break;
            
            case FeedbackStyle.RumbleMedium:
                timeInMs = 150;
                strength = 0.15f * (int)ForceFeedbackLevel;
                break;
            
            case FeedbackStyle.RumbleHeavy:
                timeInMs = 200;
                strength = 0.25f * (int)ForceFeedbackLevel;
                break;
        }

        if (strength < 0.001f) return;
        
        SDL_PlayHapticRumble(_haptic.Pointer, strength, timeInMs);
    }

    public unsafe void Dispose()
    {
        if (_haptic != null && _haptic.Pointer != null)
        {
            SDL_CloseHaptic(_haptic.Pointer);
        }
    }
}
#endif