using System.Windows.Input;

namespace Ssit.CrossX2.Framework.UI;

public interface IPageCommandsSource
{
    ICommand BackCommand => null;
    ICommand MenuCommand => null;
}