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
        
            foreach (var it in el.Items)
            {
                nativeMenuItem.Menu.Add(GenerateNativeItem(it));
            }

            return nativeMenuItem;
        }
    }
}