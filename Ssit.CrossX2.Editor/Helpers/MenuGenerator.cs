using System.Collections.Specialized;
using Avalonia.Controls;
using Ssit.CrossX2.Editor.Models;

namespace Ssit.CrossX2.Editor.Helpers
{
    public static class MenuGenerator
    {
        public static NativeMenu GenerateNativeMenu(MenuItemModel[] list)
        {
            var nativeMenu = new NativeMenu();

            foreach (var el in list)
            {
                nativeMenu.Add(GenerateNativeItem(el));
            }
        
            return nativeMenu;
        }
    
        private static NativeMenuItemBase GenerateNativeItem(MenuItemModel el)
        {
            if (el.Header == "-") return new NativeMenuItemSeparator();

            if (el.Items == null)
            {
                return new NativeMenuItem(el.Header)
                {
                    Command = el.Command,
                    CommandParameter = el.CommandParameter
                };
            }

            var nativeMenuItem = new NativeMenuItem(el.Header);
            nativeMenuItem.Menu = new NativeMenu();

            FillNativeMenu(nativeMenuItem.Menu, el);

            // Live sub menus (Recent Files) are rebuilt when their items change.
            if (el.Items is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += (_, _) => FillNativeMenu(nativeMenuItem.Menu, el);
            }

            return nativeMenuItem;
        }

        private static void FillNativeMenu(NativeMenu menu, MenuItemModel el)
        {
            menu.Items.Clear();

            foreach (var it in el.Items)
            {
                menu.Add(GenerateNativeItem(it));
            }
        }
    }
}