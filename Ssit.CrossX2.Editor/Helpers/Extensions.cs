using System.Numerics;
using System.Text;
using SkiaSharp;
using Ssit.CrossX2.Framework;

namespace Ssit.CrossX2.Editor.Helpers
{
    public static class Extensions
    {
        public static SKColor ToSkia(this RgbaColor color) => new(color.R, color.G, color.B, color.A);

        public static SKRect ToSkia(this RectangleF rect) => SKRect.Create(rect.X, rect.Y, rect.Width, rect.Height);
        
        public static SKPoint ToSkia(this Vector2 v) => new(v.X, v.Y);

        /// <summary>True when <paramref name="inner"/> lies entirely within <paramref name="outer"/> (edges may touch).</summary>
        public static bool ContainsRect(this RectangleF outer, RectangleF inner) =>
            inner.X >= outer.X && inner.Y >= outer.Y && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom;

        /// <summary>
        /// Converts CamelCase identifier into display text, e.g. "MovingPlatform" -> "Moving Platform".
        /// Acronyms are kept together: "UIScale" -> "UI Scale".
        /// </summary>
        public static string ToDisplayName(this string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return identifier;

            var name = new StringBuilder(identifier.Length + 8);
            for (var i = 0; i < identifier.Length; i++)
            {
                var ch = identifier[i];
                if (i > 0 && char.IsUpper(ch))
                {
                    var prev = identifier[i - 1];
                    var nextIsLower = i + 1 < identifier.Length && char.IsLower(identifier[i + 1]);

                    if (char.IsLower(prev) || char.IsDigit(prev) || (char.IsUpper(prev) && nextIsLower))
                    {
                        name.Append(' ');
                    }
                }

                name.Append(ch);
            }

            return name.ToString();
        }
    }
}