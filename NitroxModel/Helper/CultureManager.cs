using System.Globalization;
using System.Threading;

namespace NitroxModel.Helper;

public static class CultureManager
{
    public static readonly CultureInfo CultureInfo = new("zh-CN");

    /// <summary>
    ///     设置简体中文文化信息，同时确保数字格式保持与Subnautica内部文件兼容。
    ///     专业版启动器默认使用简体中文界面，但保持英文数字格式以确保文件解析正确。
    /// </summary>
    public static void ConfigureCultureInfo()
    {
        // 虽然我们使用中文文化信息，但仍需保持英文数字格式以确保文件解析正确
        CultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        CultureInfo.NumberFormat.NumberGroupSeparator = ",";

        Thread.CurrentThread.CurrentCulture = CultureInfo;
        Thread.CurrentThread.CurrentUICulture = CultureInfo;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo;
    }
}
