using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Ssit.CrossX2.Editor.Models
{
    public class MenuItemModel
    {
        public string Header { get; }
        public ICommand Command { get; }
        public object CommandParameter { get; }

        public IList<MenuItemModel> Items { get; }

        public MenuItemModel(string header, ICommand command, object commandParameter = null)
        {
            Header = header;
            Command = command;
            CommandParameter = commandParameter;
        }

        public MenuItemModel(string header, IEnumerable<MenuItemModel> subItems)
        {
            Header = header;
            // A live collection (e.g. Recent Files) is kept as is, so the menu follows its changes.
            Items = subItems as IList<MenuItemModel> ?? subItems?.ToArray();
        }

        public MenuItemModel()
        {
            Header = "-";
        }
    }
}