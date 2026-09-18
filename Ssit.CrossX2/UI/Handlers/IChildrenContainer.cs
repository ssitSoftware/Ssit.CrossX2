namespace Ssit.CrossX2.UI.Handlers;

public interface IChildrenContainer
{
    IReadOnlyList<ViewHandler> Children { get; }
}