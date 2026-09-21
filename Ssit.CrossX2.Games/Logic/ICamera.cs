using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Logic;

public interface IPositionObject
{
    Vector2 Position { get; }
}

public interface ICamera
{
    Vector2 LookAt { get; }
    void SetPrimaryTarget(IPositionObject positionObject, Vector2 offset, float followFactor);
    void SetTemporaryTarget(IPositionObject body, Vector2 offset, float followFactor, Action onFocused, TimeSpan returnAfter);
    void RemoveTemporaryTarget();
    void SetCameraWindow(int? width, int? height);
    void Update(float dt);
}