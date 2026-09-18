namespace Ssit.CrossX2.UI.Services;

public interface IUiActionDispatcher
{
    void Enqueue(Action action);
}