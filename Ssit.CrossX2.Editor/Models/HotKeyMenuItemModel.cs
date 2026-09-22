using System.Windows.Input;

namespace Ssit.CrossX2.Editor.Models
{
    public class HotKeyMenuItemModel
    {
        public HotKeyMenuItemModel(string hotKey, ICommand command, object commandParameter = null)
        {
            Command = command;
            CommandParameter = commandParameter;
            HotKey = hotKey;
        }

        public ICommand Command { get; }
        public object CommandParameter { get; }
        public string HotKey { get; }
    }
}