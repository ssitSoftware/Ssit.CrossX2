using System.Windows.Input;

namespace Ssit.CrossX2.Commands;

public interface IAsyncCommand : ICommand
{
    Task ExecuteAsync(object parameter);
}