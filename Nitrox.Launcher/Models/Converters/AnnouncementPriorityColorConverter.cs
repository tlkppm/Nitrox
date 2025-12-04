using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Nitrox.Launcher.Models.Design;

namespace Nitrox.Launcher.Models.Converters;

public class AnnouncementPriorityColorConverter : IValueConverter
{
    public static readonly AnnouncementPriorityColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AnnouncementPriority priority)
        {
            return priority switch
            {
                AnnouncementPriority.Critical => Color.FromRgb(220, 38, 38), // 深红色 - 严重
                AnnouncementPriority.High => Color.FromRgb(249, 115, 22), // 橙色 - 高
                AnnouncementPriority.Medium => Color.FromRgb(202, 138, 4), // 金色 - 中
                AnnouncementPriority.Normal => Color.FromRgb(37, 99, 235), // 主题蓝色 - 普通
                AnnouncementPriority.Low => Color.FromRgb(148, 163, 184), // 浅灰色 - 低
                _ => Color.FromRgb(37, 99, 235)
            };
        }
        return Color.FromRgb(37, 99, 235); // 默认主题蓝色
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

