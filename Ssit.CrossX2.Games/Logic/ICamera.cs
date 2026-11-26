using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Logic;

public interface ICamera
{
    Vector2 LookAt { get; }

    /// <summary>
    /// World-space rectangle (in tiles) visible on the screen in the last rendered frame.
    /// Empty when the camera doesn't know what is visible.
    /// </summary>
    RectangleF VisibleBounds => default;
    void SetPrimaryTarget(IPositionObject positionObject, Vector2 offset, float followFactor);
    
    /// <summary>
    /// Moves the camera immediately to the primary target (e.g. after the player was moved by a checkpoint restore).
    /// </summary>
    void SnapToPrimaryTarget()
    {
    }
    void SetTemporaryTarget(IPositionObject body, Vector2 offset, float followFactor, Action onFocused, TimeSpan returnAfter);
    void RemoveTemporaryTarget();
    void SetCameraWindow(int? width, int? height);
    void Update(float dt);

    /// <summary>
    /// Shakes the camera for <paramref name="duration"/> seconds: <see cref="LookAt"/> jumps to a new random offset of at
    /// most <paramref name="amplitude"/> map units on each axis, 30 times a second. A shake during another one keeps the
    /// stronger amplitude and the longer remaining time.
    /// </summary>
    void Shake(float amplitude, float duration);
}