using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Nitrox.Launcher.Models.Design;

namespace Nitrox.Launcher.Models.Converters;

public class AnnouncementTypeIconConverter : IValueConverter
{
    public static readonly AnnouncementTypeIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AnnouncementType type)
        {
            return type switch
            {
                AnnouncementType.Info => "ℹ️",
                AnnouncementType.Warning => "⚠️",
                AnnouncementType.Feature => "🎉",
                AnnouncementType.Tips => "💡",
                AnnouncementType.Bugfix => "🐟",
                _ => "ℹ️"
            };
        }
        return "ℹ️";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
