using System.Windows.Input;

namespace Ssit.CrossX2.Framework.Commands;

public interface IAsyncCommand : ICommand
{
    Task ExecuteAsync(object parameter);
}