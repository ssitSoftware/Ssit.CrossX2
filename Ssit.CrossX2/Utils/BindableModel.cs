using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ssit.CrossX2.Utils;

public class BindableModel: INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }

    protected virtual bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        
        OnPropertyChanged(propertyName);
        return true;
    }
}