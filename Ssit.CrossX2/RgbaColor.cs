using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Ssit.CrossX2;

/// <summary>
/// Represents a color in the RGBA (Red, Green, Blue, Alpha) color space.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[DebuggerDisplay("RgbaColor = ({R}, {G}, {B}, {A})")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public readonly partial struct RgbaColor(byte red, byte green, byte blue, byte alpha = 255)
    : IEquatable<RgbaColor>
{
    /// <summary>
    /// Represents the red component of the color in the RGBA color space.
    /// </summary>
    public readonly byte R = red;

    /// <summary>
    /// Represents the green component of the color in the RGBA color space.
    /// </summary>
    public readonly byte G = green;

    /// <summary>
    /// Represents the blue component of the color in the RGBA color space.
    /// </summary>
    public readonly byte B = blue;

    /// <summary>
    /// Represents the alpha component of the RGBA color,
    /// specifying the transparency level. A value of 255
    /// represents full opacity, while a value of 0 represents
    /// full transparency.
    /// </summary>
    public readonly byte A = alpha;

    /// <summary>
    /// Represents the normalized red component of the color in the RGBA color space.
    /// The value is in the range of 0.0 to 1.0.
    /// </summary>
    public float Rf => R / 255.0f;

    /// <summary>
    /// Represents the normalized green component of the color in the RGBA color space.
    /// The value is in the range of 0.0 to 1.0.
    /// </summary>
    public float Gf => G / 255.0f;

    /// <summary>
    /// Represents the normalized blue component of the color in the RGBA color space.
    /// The value is in the range of 0.0 to 1.0.
    /// </summary>
    public float Bf => B / 255.0f;

    /// <summary>
    /// Represents the normalized alpha component of the color in the RGBA color space.
    /// The value is in the range of 0.0 to 1.0, where 0.0 is full transparency and 1.0 is full opacity.
    /// </summary>
    public float Af => A / 255.0f;

    public RgbaColor(long color) : this((byte)(color & 0xff), (byte)((color >> 8) & 0xff), (byte)((color >> 16) & 0xff), (byte)((color >> 24) & 0xff))
    {
    }

    public RgbaColor(float red, float green, float blue, float alpha = 1.0f) : this((byte)Math.Min(255, red * 255), (byte)Math.Min(255, green * 255), (byte)Math.Min(255, blue * 255), (byte)Math.Min(255, alpha * 255))
    {
    }

    /// <summary>
    /// Blends this color with another color based on a specified mix ratio.
    /// </summary>
    /// <param name="other">The other RgbaColor to blend with.</param>
    /// <param name="mix">The mix ratio, where 0 results in the original color and 1 results in the other color.</param>
    /// <returns>A new RgbaColor that is the result of the blend operation.</returns>
    public RgbaColor Mix(RgbaColor other, float mix)
    {
        var red = Rf * (1 - mix) + other.Rf * mix;
        var green = Gf * (1 - mix) + other.Gf * mix;
        var blue = Bf * (1 - mix) + other.Bf * mix;
        var alpha = Af * (1 - mix) + other.Af * mix;
        
        return new RgbaColor(red, green, blue, alpha);
    }
    
    public int ToInt32()
    {
        long argb = B | (G << 8) | (R << 16) | (A << 24);
        return (int)argb;
    }

    public uint ToUInt32()
    {
        long argb = R | (G << 8) | (B << 16) | (A << 24);
        return (uint)argb;
    }
    
    public uint ToUInt32Inverted()
    {
        long argb = A | (B << 8) | (G << 16) | (R << 24);
        return (uint)argb;
    }
    
    public RgbaColor Invert() => new(255 - R, 255 - G, 255 - B, A);

    public (float h, float s, float l) ToHsl()
    {
        var cMax = MathF.Max(Rf, MathF.Max(Gf, Bf));
        var cMin =  MathF.Min(Rf, MathF.Min(Gf, Bf));
        var delta = cMax - cMin;

        var l = (cMax + cMin) / 2;
        var s = delta != 0 ? delta / (1 - MathF.Abs(2 * l - 1)) : 0;
        var h = 0f;

        if (delta == 0)
        {
            h = 0;
        }
        else if (Math.Abs(cMax - Rf) < float.Epsilon)
        {
            h = 60 * ((Gf - Bf) / delta) % 6;
        }
        else if (Math.Abs(cMax - Gf) < float.Epsilon)
        {
            h = 60 * ((Bf - Rf) / delta) + 120;
        }
        else if (Math.Abs(cMax - Bf) < float.Epsilon)
        {
            h = 60 * ((Rf - Gf) / delta) + 240;
        }

        return (h, s, l);
    }

    public static RgbaColor FromHsl(float h, float s, float l)
    {
        h %= 360;
        s = MathF.Max(0, MathF.Min(1, s));
        l = MathF.Max(0, MathF.Min(1, l));
        
        var c = (1 - MathF.Abs(2 * l - 1)) * s;
        var x = c * (1 - MathF.Abs(h / 60 % 2 - 1));
        var m = l - c / 2;
        
        if (h < 60)
        {
            return new RgbaColor(c + m, x + m, m);
        }

        if (h < 120)
        {
            return new RgbaColor(x + m, c + m, m);
        }

        if (h < 180)
        {
            return new RgbaColor(m, c + m, x + m);
        }

        if (h < 240)
        {
            return new RgbaColor(m, x + m, c + m);
        }

        if ( h < 300)
        {
            return new RgbaColor(x + m, m, c + m);
        }

        return new RgbaColor(c + m, m, x + m);
    }

/*
    public RgbaColor InvertHsl(bool forceDifferent = false)
    {
        var (h,s,l) = ToHsl();

        h %= 360;
        s = MathF.Max(0, MathF.Min(1, s));
        l = MathF.Max(0, MathF.Min(1, l));

        var nh = 360 - h;
        var nl  = 1 - l;

        if (!forceDifferent || !(MathF.Abs(l - 0.5f) < 0.2) || !(MathF.Abs(nh - h) < 30)) return FromHsl(nh, s, nl);

        nl += 0.25f;
        nh = (nh + 180) % 360;

        if (s < 0.25f)
        {
            s = 0.25f;
        }

        return FromHsl(nh, s, nl);
    }
*/
    
    public static RgbaColor operator * (RgbaColor color1, RgbaColor color2)
    {
        return new RgbaColor(color1.Rf * color2.Rf, color1.Gf * color2.Gf, color1.Bf * color2.Bf, color1.Af * color2.Af);
    }
    
    public RgbaColor WithOpacity(float opacity) => new(Rf, Gf, Bf, opacity);
    
    public static RgbaColor operator *(RgbaColor color, float multiply) => new(color.Rf * multiply, color.Gf * multiply, color.Bf * multiply, color.Af * multiply);
    public static RgbaColor operator *(RgbaColor color, double multiply) => color * (float)multiply;

    public bool Equals(RgbaColor other) => R == other.R && G == other.G && B == other.B && A == other.A;

    public static bool operator ==(RgbaColor c1, RgbaColor c2) => c1.Equals(c2);

    public static bool operator !=(RgbaColor c1, RgbaColor c2) => !c1.Equals(c2);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is RgbaColor color && Equals(color);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = R.GetHashCode();
            hashCode = (hashCode * 397) ^ G.GetHashCode();
            hashCode = (hashCode * 397) ^ B.GetHashCode();
            hashCode = (hashCode * 397) ^ A.GetHashCode();
            return hashCode;
        }
    }
    
    public RgbaColor AsPremultiplied() => FromNonPremultiplied(R, G, B, A);

    public float DistanceTo(RgbaColor color)
    {
        var dr = Math.Abs(color.R - R);
        var dg = Math.Abs(color.G - G);
        var db = Math.Abs(color.B - B);

        return MathF.Sqrt(dr * dr + dg * dg + db * db);
    }

    /// <summary>
    /// Creates a new RgbaColor from a 32-bit unsigned integer representation.
    /// </summary>
    /// <param name="intColor">The 32-bit unsigned integer that encodes the RGBA color.</param>
    /// <param name="premultiply">Determines if the color should be premultiplied by its alpha value.</param>
    /// <returns>An RgbaColor instance representing the decoded RGBA color.</returns>
    public static RgbaColor FromRgba(uint intColor, bool premultiply = true)
    {
        var a = (intColor >> 24) & 0xff;
        var b = (intColor >> 16) & 0xff;
        var g = (intColor >> 8) & 0xff;
        var r = intColor & 0xff;

        var color = new RgbaColor((byte)r, (byte)g, (byte)b, (byte)a);
        return premultiply ? color.AsPremultiplied() : color;
    }
    
    /// <summary>
    /// Creates a new RgbaColor from a 32-bit unsigned integer representation.
    /// </summary>
    /// <param name="intColor">The 32-bit unsigned integer that encodes the RGBA color.</param>
    /// <param name="premultiply">Determines if the color should be premultiplied by its alpha value.</param>
    /// <returns>An RgbaColor instance representing the decoded RGBA color.</returns>
    public static RgbaColor FromBgra(uint intColor, bool premultiply = true)
    {
        var a = (intColor >> 24) & 0xff;
        var r = (intColor >> 16) & 0xff;
        var g = (intColor >> 8) & 0xff;
        var b = intColor & 0xff;

        var color = new RgbaColor((byte)r, (byte)g, (byte)b, (byte)a);
        return premultiply ? color.AsPremultiplied() : color;
    }

    public static implicit operator RgbaColor(string name)
    {
        if (!name.StartsWith('#') || name.Length is not (7 or 9)) return global::Ssit.CrossX2.RgbaColor.Transparent;

        var hexColor = name.AsSpan(1);
            
        if (!int.TryParse(hexColor, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var color))
            return global::Ssit.CrossX2.RgbaColor.Transparent;
            
        var a = color >> 24 & 0xff;
        var r = color >> 16 & 0xff;
        var g = color >> 8 & 0xff;
        var b = color & 0xff;

        if(name.Length == 7)
        {
            a = 255;
        }

        return new RgbaColor((byte)r, (byte)g, (byte)b) * (a / 255.0f);
    }

    public static implicit operator RgbaColor(uint intColor)
    {
        if ((intColor & 0xff000000) == 0)
        {
            intColor |= 0xff000000;
        }

        return FromBgra(intColor);
    }
    
    /// <summary>
    /// Converts this RgbaColor instance to a System.Drawing.Color object.
    /// The resulting Color object will have the same red, green, blue, and alpha components
    /// as this RgbaColor instance.
    /// </summary>
    /// <returns>
    /// A System.Drawing.Color object representing the same color as this RgbaColor instance.
    /// </returns>
    public Color ToDrawingColor() => Color.FromArgb(A, R, G, B);

    /// <summary>
    /// Implicitly converts a RgbaColor instance to a System.Drawing.Color instance.
    /// </summary>
    /// <param name="color">The RgbaColor color instance to be converted.</param>
    /// <returns>A System.Drawing.Color instance representing the specified RgbaColor instance.</returns>
    public static implicit operator Color(RgbaColor color) => color.ToDrawingColor();
    
    /// <summary>
    /// Implicitly converts a System.Drawing.Color instance to an RgbaColor instance.
    /// The conversion takes the red, green, blue, and alpha components from the System.Drawing.Color.
    /// </summary>
    /// <param name="color">The System.Drawing.Color instance to be converted.</param>
    /// <returns>An RgbaColor instance representing the specified System.Drawing.Color instance.</returns>
    public static implicit operator RgbaColor(Color color) => new(color.R, color.G, color.B, color.A);

    /// <summary>
    /// Creates a new RgbaColor from non-premultiplied red, green, blue, and alpha values.
    /// </summary>
    /// <param name="r">The red component of the color, from 0 to 255.</param>
    /// <param name="g">The green component of the color, from 0 to 255.</param>
    /// <param name="b">The blue component of the color, from 0 to 255.</param>
    /// <param name="a">The alpha component of the color, from 0 to 255.</param>
    /// <returns>A new RgbaColor with the specified non-premultiplied components.</returns>
    public static RgbaColor FromNonPremultiplied(byte r, byte g, byte b, byte a) => FromNonPremultiplied(r / 255f, g / 255f, b / 255f, a / 255f);
    
    /// <summary>
    /// Creates a new RgbaColor instance from non-premultiplied RGBA values.
    /// </summary>
    /// <param name="r">The red component, in the range [0, 1].</param>
    /// <param name="g">The green component, in the range [0, 1].</param>
    /// <param name="b">The blue component, in the range [0, 1].</param>
    /// <param name="a">The alpha component, in the range [0, 1].</param>
    /// <returns>A new RgbaColor with premultiplied RGB values.</returns>
    public static RgbaColor FromNonPremultiplied(float r, float g, float b, float a) => new(r * a, g * a, b * a, a);
}