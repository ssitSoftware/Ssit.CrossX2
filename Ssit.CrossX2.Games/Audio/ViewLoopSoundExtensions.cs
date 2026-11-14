using System.Numerics;
using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Games.Logic;

namespace Ssit.CrossX2.Framework.Games.Audio;

/// <summary>
/// Helpers for looped sounds whose volume depends on where the emitter is on screen.
/// </summary>
public static class ViewLoopSoundExtensions
{
    /// <summary>
    /// Volume factor based on the distance from the camera (view) center to <paramref name="position"/>:
    /// 1 at the center, 0 at a distance of twice the view height (logical units) or more.
    /// The falloff is quadratic: slow near the center, faster towards the max distance.
    /// </summary>
    public static float GetViewVolume(this ICamera camera, Vector2 position)
    {
        var bounds = camera.VisibleBounds;
        var maxDistance = bounds.Height * 2;
        if (maxDistance <= 0) return 0;

        var distance = (position - bounds.Center).Length() / maxDistance;
        if (distance >= 1) return 0;

        return 1 - distance * distance;
    }

    /// <summary>
    /// Updates a looped sound instance from its emitter's position relative to the camera. Call every update.
    /// Stops the sound when <see cref="GetViewVolume"/> drops to 0 (emitter at/beyond the max distance),
    /// (re)starts it when the emitter gets back within it, and adjusts its volume (full at the view center).
    /// Applies a Doppler shift (<see cref="ISoundEffectInstance.ApplyDoppler"/>) on top of <paramref name="pitch"/>.
    /// </summary>
    /// <param name="dt">Time since the previous update in seconds.</param>
    /// <param name="speedOfSound">Speed of sound in tiles/s; 0 or less disables the Doppler effect.</param>
    public static void UpdateViewLoop(this ISoundEffectInstance instance, ICamera camera, float dt,
        float volume = 1, float pitch = 1, float speedOfSound = ISoundEffectInstance.DefaultSpeedOfSound)
    {
        var emitter = instance.Emitter;
        var factor = emitter is not null ? camera.GetViewVolume(emitter.Position) : 0;

        if (factor <= 0)
        {
            if (instance.IsPlaying)
            {
                instance.Stop();
            }
            return;
        }

        // Setting parameters also re-applies emitter/listener (pan) for the current emitter position.
        instance.Parameters = new SoundParameters(volume * factor, pitch);

        if (!instance.IsPlaying)
        {
            instance.Play(true);
        }

        instance.ApplyDoppler(dt, speedOfSound);
    }
}
