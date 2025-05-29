using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
namespace Heatwise.Converters;
public class BoolToBackgroundConverter : IValueConverter
{
public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
{
    return (bool)value
        ? new SolidColorBrush(new Color(128, Colors.Black.R, Colors.Black.G, Colors.Black.B))
        : Brushes.Transparent;
}

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}