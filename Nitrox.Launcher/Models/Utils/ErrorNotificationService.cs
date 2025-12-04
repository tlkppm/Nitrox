using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Nitrox.Launcher.Models.Utils;

namespace Nitrox.Launcher.Models.Utils;

/// <summary>
/// 错误通知服务 - 在主界面显示用户友好的错误消息
/// </summary>
public static class ErrorNotificationService
{
    private static Window? MainWindow;
    
    /// <summary>
    /// 设置主窗口引用
    /// </summary>
    public static void SetMainWindow(Window mainWindow)
    {
        MainWindow = mainWindow;
        
        // 检查是否有待显示的错误
        if (UserFriendlyErrorHandler.HasErrors())
        {
            Task.Delay(2000).ContinueWith(_ => ShowPendingErrors());
        }
    }
    
    /// <summary>
    /// 显示待处理的错误
    /// </summary>
    private static void ShowPendingErrors()
    {
        if (MainWindow != null)
        {
            UserFriendlyErrorHandler.ShowErrorsInUI(MainWindow);
        }
    }
    
    /// <summary>
    /// 立即显示错误
    /// </summary>
    public static void ShowErrors()
    {
        if (MainWindow != null)
        {
            UserFriendlyErrorHandler.ShowErrorsInUI(MainWindow);
        }
    }
}
