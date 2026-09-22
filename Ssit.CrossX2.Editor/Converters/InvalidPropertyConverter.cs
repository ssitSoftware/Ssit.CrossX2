using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Ssit.CrossX2.Framework;

namespace Ssit.CrossX2.Editor.Converters;

public class InvalidPropertyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (true.Equals(value))
        {
            return Brushes.IndianRed;
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class RgbaColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is RgbaColor color)
        {
            return new Color(color.A, color.R, color.G, color.B);
        }

        return Colors.Magenta;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new RgbaColor(color.R, color.G, color.B, color.A);
        }

        return RgbaColor.Magenta;
    }
}