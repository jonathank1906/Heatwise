// EnumToBoolConverter.cs
using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Heatwise.Converters
{
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;
            
            return value.ToString() == parameter.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;
            
            return (bool)value ? Enum.Parse(targetType, parameter.ToString()!) : null;
        }
    }
}