using System.Windows.Input;

namespace Ssit.CrossX2.Framework.Commands;

public class SyncCommand: ICommand
{
    public static readonly ICommand Empty = new SyncCommand(o => { });
    
    private readonly Func<object, bool> _canExecute;
    private readonly Action<object> _execute;
    
    public event EventHandler CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public SyncCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }
    
    public SyncCommand(Action execute, Func<bool> canExecute = null)
    {
        if(execute is null) throw new ArgumentNullException(nameof(execute));
        
        _execute = _ => execute();
        _canExecute = _ => canExecute?.Invoke() ?? true;
    }
    
    public bool CanExecute(object parameter = null)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object parameter = null)
    {
        if (CanExecute(parameter))
        {
            _execute?.Invoke(parameter);
        }
    }
}

public class SyncCommand<TParameter>(Action<TParameter> execute, Func<TParameter, bool> canExecute = null)
    : SyncCommand(o => execute((TParameter)o),
        o => canExecute?.Invoke((TParameter)o) ?? true);