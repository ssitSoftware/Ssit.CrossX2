using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Logic;

public interface ICamera
{
    Vector2 LookAt { get; }
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
}