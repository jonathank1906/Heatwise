using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Heatwise.Converters;

public class BoolToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? new SolidColorBrush(Color.FromArgb(0xD0, 0x00, 0x00, 0x00)) : Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}