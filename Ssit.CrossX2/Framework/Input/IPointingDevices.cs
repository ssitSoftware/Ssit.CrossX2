using System.Numerics;
using Ssit.CrossX2.Framework.Input.Internal;

namespace Ssit.CrossX2.Framework.Input;

public interface IPointingDevices
{
    bool LockMouseInWindow { get; set; }
    PointingDevicesMode Mode { get; set; }
    IReadOnlyList<Pointer> Pointers { get; }
    Pointer GetPointer(int id);
    Vector2? HoverPosition { get; }
    bool ShowHoverPointer { get; set; }
    void PushTransform(Matrix3x2 transform);
    void PopTransform();
    void SetHoverPosition(Vector2 position);
}