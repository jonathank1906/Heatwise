using System;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;
using System.Diagnostics;

namespace Heatwise.Converters
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                try
                {
                    // Handle both formats: #RRGGBB and #AARRGGBB
                    if (colorString.Length == 7 || colorString.Length == 9)
                    {
                        var color = Color.Parse(colorString);
                        return new SolidColorBrush(color); // Return SolidColorBrush
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error converting color string '{colorString}': {ex.Message}");
                }
            }

            // Return a default SolidColorBrush if conversion fails
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var color = brush.Color;
                // Convert back to #AARRGGBB format string
                return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            }
            return "#FFFFFFFF"; // Default white color
        }
    }
}