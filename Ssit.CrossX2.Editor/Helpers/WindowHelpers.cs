using Avalonia;
using Avalonia.Controls;

namespace Ssit.CrossX2.Editor.Helpers
{
    public static class WindowHelpers
    {
        public static void FitDialog(Window window, Control root)
        {
            root.InvalidateMeasure();
            root.Measure(window.ClientSize);
            window.Height = root.DesiredSize.Height;
            window.Width = root.DesiredSize.Width;

            var bounds = window.Screens.ScreenFromPoint(window.Position).Bounds;
        
            var x = bounds.Center.X - window.Width / 2;
            var y = (bounds.Y + bounds.Center.Y - window.Height) / 2;

            window.Position = new PixelPoint((int)x, (int)y);
        }
    }
}