using System.Windows.Input;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.Games.Logic;

public interface IGameDialogsUi: IDisposable
{
    event Action<int> FocusElement;
    ICommand ReplyCommand { get; }
    SharedBool Visible { get; }
    SharedString CurrentText { get; }
    
    IReadOnlyList<SharedBool> ReplyOptionVisible { get; }
    IReadOnlyList<SharedString> ReplyOptions { get; }
}