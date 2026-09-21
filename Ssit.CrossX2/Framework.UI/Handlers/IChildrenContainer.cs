namespace Ssit.CrossX2.Framework.UI.Handlers;

public interface IChildrenContainer
{
    IReadOnlyList<ViewHandler> Children { get; }
}