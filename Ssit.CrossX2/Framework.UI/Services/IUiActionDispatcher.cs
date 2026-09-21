namespace Ssit.CrossX2.Framework.UI.Services;

public interface IUiActionDispatcher
{
    void Enqueue(Action action);
}