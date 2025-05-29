using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia;
using Heatwise.Interfaces;
namespace Heatwise.Converters;
public class PopupStartupLocationToMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PopupStartupLocation location)
        {
            switch (location)
            {
                case PopupStartupLocation.Center:
                    return new Thickness(0);
                case PopupStartupLocation.Custom:
                    return new Thickness(50, 50, 0, 0); // Example custom
            }
        }
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}