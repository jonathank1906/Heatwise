using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Heatwise.Converters;


public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && !b; // Return true for collapsed, false for visible
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}