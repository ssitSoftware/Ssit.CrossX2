using System.Numerics;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Services;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public interface IInputConsumer
{
    void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context);
    bool ProcessInput(IReadOnlyList<Pointer> pointers, IInputContext context);
    void CancelPointer(int pointerId, IInputContext context);
}