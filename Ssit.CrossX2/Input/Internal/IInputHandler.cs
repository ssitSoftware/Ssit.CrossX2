using System.Numerics;

namespace Ssit.CrossX2.Input.Internal;

public interface IInputHandler
{
    void OnTouch(ulong id, ButtonState state, Vector2? position);
}