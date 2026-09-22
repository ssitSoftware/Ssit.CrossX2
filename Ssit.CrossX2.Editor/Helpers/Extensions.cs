using System.Numerics;
using SkiaSharp;
using Ssit.CrossX2.Framework;

namespace Ssit.CrossX2.Editor.Helpers
{
    public static class Extensions
    {
        public static SKColor ToSkia(this RgbaColor color) => new(color.R, color.G, color.B, color.A);

        public static SKRect ToSkia(this RectangleF rect) => SKRect.Create(rect.X, rect.Y, rect.Width, rect.Height);
        
        public static SKPoint ToSkia(this Vector2 v) => new(v.X, v.Y);
    }
}