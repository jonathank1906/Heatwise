using System;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;
using System.Diagnostics;

namespace Sem2Proj.Converters
{
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                try
                {
                    // Handle both formats: #RRGGBB and #AARRGGBB
                    if (colorString.Length == 7) // #RRGGBB
                    {
                        return Color.Parse(colorString);
                    }
                    else if (colorString.Length == 9) // #AARRGGBB
                    {
                        // Avalonia's Color.Parse handles #AARRGGBB format directly
                        return Color.Parse(colorString);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error converting color string '{colorString}': {ex.Message}");
                }
            }
            
            // Return default color if conversion fails
            return Colors.White;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                // Convert back to #AARRGGBB format string
                return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            }
            return "#FFFFFFFF"; // Default white color
        }
    }
}