using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Nitrox.Launcher.Models.Converters;

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isHighlighted)
        {
            return isHighlighted ? Brush.Parse("#FF4B9FE1") : Brush.Parse("#666666");
        }
        return Brush.Parse("#666666");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
