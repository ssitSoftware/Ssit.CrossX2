using System.Runtime.InteropServices;
using Avalonia.Input;

namespace Ssit.CrossX2.Editor.Helpers
{
    public static class HotKeys
    {
        public static KeyGesture FileNew { get; private set; }
        public static KeyGesture FileOpen { get; private set; }
        public static KeyGesture FileSave { get; private set; }
        public static KeyGesture FileSaveAs { get; private set; }
        public static KeyGesture EditUndo { get; private set; }
        public static KeyGesture EditRedo { get; private set; }
        public static KeyGesture EditRedoAlt { get; private set; }
    
        public static KeyGesture EditorZoomIn { get; private set; }
        public static KeyGesture EditorZoomOut { get; private set; }
    
        public static KeyGesture TilesetZoomIn { get; private set; }
        public static KeyGesture TilesetZoomOut { get; private set; }
    
        public static KeyGesture ToolsInsertTiles { get; private set; }
        public static KeyGesture ToolsEraser { get; private set; }
        
        public static KeyGesture HorizontalFlip { get; private set; }

        static HotKeys()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                InitializeMacOsHotKeys();
            }
            else
            {
                InitializeWindowsKeys();
            }
        }

        private static void InitializeWindowsKeys()
        {
            FileNew = KeyGesture.Parse("Ctrl+N");
            FileOpen = KeyGesture.Parse("Ctrl+O");
            FileSave = KeyGesture.Parse("Ctrl+S");
            FileSaveAs = KeyGesture.Parse("Ctrl+Shift+S");
            
            EditUndo = KeyGesture.Parse("Ctrl+Z");
            EditRedo = KeyGesture.Parse("Ctrl+Y");
            EditRedoAlt = KeyGesture.Parse("Ctrl+Shift+Z");
            
            ToolsInsertTiles = KeyGesture.Parse("Ctrl+Shift+T");
            ToolsEraser = KeyGesture.Parse("Ctrl+Shift+E");
        
            EditorZoomIn = KeyGesture.Parse("Ctrl++");
            EditorZoomOut = KeyGesture.Parse("Ctrl+-");
            TilesetZoomIn = KeyGesture.Parse("Ctrl+Shift++");
            TilesetZoomOut = KeyGesture.Parse("Ctrl+Shift+-");
            HorizontalFlip = KeyGesture.Parse("Ctrl+F");
        }

        private static void InitializeMacOsHotKeys()
        {
            FileNew = KeyGesture.Parse("Meta+N");
            FileOpen = KeyGesture.Parse("Meta+O");
            FileSave = KeyGesture.Parse("Meta+S");
            FileSaveAs = KeyGesture.Parse("Meta+Shift+S");
            
            EditUndo = KeyGesture.Parse("Meta+Z");
            EditRedo = KeyGesture.Parse("Meta+Y");
            EditRedoAlt = KeyGesture.Parse("Meta+Shift+Z");
            
            ToolsInsertTiles = KeyGesture.Parse("Meta+Shift+T");
            ToolsEraser = KeyGesture.Parse("Meta+Shift+E");
        
            EditorZoomIn = KeyGesture.Parse("Meta++");
            EditorZoomOut = KeyGesture.Parse("Meta+-");
            TilesetZoomIn = KeyGesture.Parse("Meta+Shift++");
            TilesetZoomOut = KeyGesture.Parse("Meta+Shift+-");
            HorizontalFlip = KeyGesture.Parse("Meta+F");
        }
    }
}