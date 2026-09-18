using System.Numerics;
using Ssit.CrossX2.Input;
using Ssit.CrossX2.UI.Services;

namespace Ssit.CrossX2.UI.Handlers;

public interface IInputConsumer
{
    void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context);
    bool ProcessInput(IReadOnlyList<Pointer> pointers, IInputContext context);
    void CancelPointer(int pointerId, IInputContext context);
}