using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Ssit.CrossX2.Editor.Models.Parameters;

namespace Ssit.CrossX2.Editor.Converters;

public class ParameterHeaderPaddingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ParameterHeaderModel ? new Thickness(0) : AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
