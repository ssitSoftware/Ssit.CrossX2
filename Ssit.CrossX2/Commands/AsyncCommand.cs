namespace Ssit.CrossX2.Commands;

public class AsyncCommand: IAsyncCommand
{
    private readonly Func<object, Task> _execute;
    private readonly Func<object, bool> _canExecute;
    private readonly Action<Exception> _onException;
    
    private bool _isExecuting;
    
    public event EventHandler CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public AsyncCommand(Func<object, Task> execute, Func<object, bool> canExecute = null,
        Action<Exception> onException = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _onException = onException;
    }
    
    public AsyncCommand(Func<Task> execute, Func<bool> canExecute = null,
        Action<Exception> onException = null)
    {
        _execute = execute != null ? _ => execute() : throw new ArgumentNullException(nameof(execute));
        _canExecute = _ => canExecute?.Invoke() ?? true;
        _onException = onException;
    }
    
    public bool CanExecute(object parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
    }

    public async void Execute(object parameter)
    {
        try
        {
            await ExecuteAsync(parameter);
        }
        catch (Exception)
        {
            // SKIP - Already called _onException;
        }
    }
    
    public async Task ExecuteAsync(object parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();

            var task = _execute?.Invoke(parameter);
            if (task != null)
            {
                await Task.Delay(4);
                await task;
            }
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
            throw;
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }
}

public class AsyncCommand<TParameter>(
    Func<TParameter, Task> execute,
    Func<TParameter, bool> canExecute = null,
    Action<Exception> onException = null)
    : AsyncCommand(o => execute((TParameter)o), o => canExecute((TParameter)o), onException);