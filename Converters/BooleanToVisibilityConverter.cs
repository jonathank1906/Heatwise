using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Sem2Proj.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b; // Return true for visible, false for collapsed
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

