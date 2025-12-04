using System;
using System.Globalization;
using NitroxModel.Logger;

namespace Nitrox.Launcher.Models.Converters;

/// <summary>
/// 检查字符串是否不为null或空的转换器
/// </summary>
public class StringNotNullOrEmptyConverter : Converter<StringNotNullOrEmptyConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool result = value is string str && !string.IsNullOrEmpty(str);
        Log.Info($"[StringNotNullOrEmptyConverter] Value: '{value}', Result: {result}");
        return result;
    }
}

