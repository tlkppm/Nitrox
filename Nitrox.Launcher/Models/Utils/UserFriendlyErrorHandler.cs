using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using NitroxModel.Logger;

namespace Nitrox.Launcher.Models.Utils;

/// <summary>
/// 用户友好的错误处理器 - 在界面中显示错误而不是仅在日志中记录
/// </summary>
public static class UserFriendlyErrorHandler
{
    private static readonly List<string> ErrorMessages = new();
    private static readonly object ErrorLock = new();

    /// <summary>
    /// 记录一个用户友好的错误消息
    /// </summary>
    public static void RecordError(string title, string description, string solution = null)
    {
        lock (ErrorLock)
        {
            var errorMessage = $"❌ {title}\n📋 {description}";
            if (!string.IsNullOrEmpty(solution))
            {
                errorMessage += $"\n💡 解决方案: {solution}";
            }
            
            ErrorMessages.Add(errorMessage);
            
            // 同时记录到日志，但用户主要看界面
            Log.Error($"用户错误: {title} - {description}");
        }
    }

    /// <summary>
    /// 记录依赖问题错误
    /// </summary>
    public static void RecordDependencyError(string missingAssembly, string context)
    {
        string title = "缺少系统组件";
        string description = $"缺少必需的系统文件: {missingAssembly}";
        string solution = GetDependencySolution(missingAssembly);
        
        RecordError(title, description, solution);
    }

    /// <summary>
    /// 记录保存文件错误
    /// </summary>
    public static void RecordSaveFileError(string saveDirectory, Exception ex)
    {
        string title = "保存文件损坏";
        string description = $"存档目录 '{Path.GetFileName(saveDirectory)}' 中的数据已损坏";
        string solution = "建议删除该存档或重新创建服务器";
        
        RecordError(title, description, solution);
        
        // 不记录完整的异常堆栈，只记录关键信息
        Log.Warn($"跳过损坏的存档: {saveDirectory} - {ex.GetType().Name}: {ex.Message}");
    }

    /// <summary>
    /// 获取所有错误消息
    /// </summary>
    public static List<string> GetErrorMessages()
    {
        lock (ErrorLock)
        {
            return new List<string>(ErrorMessages);
        }
    }

    /// <summary>
    /// 清除所有错误消息
    /// </summary>
    public static void ClearErrors()
    {
        lock (ErrorLock)
        {
            ErrorMessages.Clear();
        }
    }

    /// <summary>
    /// 检查是否有错误
    /// </summary>
    public static bool HasErrors()
    {
        lock (ErrorLock)
        {
            return ErrorMessages.Count > 0;
        }
    }

    /// <summary>
    /// 在主界面显示错误通知
    /// </summary>
    public static void ShowErrorsInUI(Window parentWindow = null)
    {
        if (!HasErrors()) return;

        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var errors = GetErrorMessages();
                var errorText = string.Join("\n\n", errors);
                
                // 创建错误对话框
                var dialog = new ContentDialog
                {
                    Title = "⚠️ 检测到问题",
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = errorText,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            MaxWidth = 500
                        },
                        MaxHeight = 400
                    },
                    PrimaryButtonText = "我知道了",
                    SecondaryButtonText = "复制错误信息",
                    DefaultButton = ContentDialogButton.Primary
                };

                if (parentWindow != null)
                {
                    var result = await dialog.ShowAsync(parentWindow);
                    if (result == ContentDialogResult.Secondary)
                    {
                        // 复制错误信息到剪贴板
                        await parentWindow.Clipboard?.SetTextAsync(errorText);
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果显示错误对话框失败，回退到日志
                Log.Error(ex, "无法显示错误对话框");
            }
        });
    }

    /// <summary>
    /// 获取依赖问题的解决方案
    /// </summary>
    private static string GetDependencySolution(string missingAssembly)
    {
        return missingAssembly.ToLower() switch
        {
            var s when s.Contains("system.security.permissions") => 
                "请安装最新的 .NET 9 运行时，或者升级到最新版本的 Visual C++ Redistributable",
            
            var s when s.Contains("newtonsoft.json") => 
                "JSON处理组件缺失，请重新安装 Nitrox 启动器",
            
            var s when s.Contains("system.text.json") => 
                "系统JSON组件缺失，请更新 .NET 运行时到最新版本",
            
            var s when s.Contains("microsoft.extensions") => 
                "Microsoft扩展组件缺失，请安装 .NET 9 运行时",
            
            _ => "请尝试重新安装 Nitrox 或更新系统运行时组件"
        };
    }

    /// <summary>
    /// 安全地尝试操作，如果失败则记录用户友好的错误
    /// </summary>
    public static T SafeExecute<T>(Func<T> operation, string operationName, T defaultValue = default(T))
    {
        try
        {
            return operation();
        }
        catch (FileNotFoundException ex) when (ex.Message.Contains("Could not load file or assembly"))
        {
            string assemblyName = ExtractAssemblyName(ex.Message);
            RecordDependencyError(assemblyName, operationName);
            return defaultValue;
        }
        catch (Exception ex)
        {
            RecordError($"{operationName}失败", ex.Message, "请检查系统环境或重新安装程序");
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全地尝试操作（无返回值版本）
    /// </summary>
    public static void SafeExecute(Action operation, string operationName)
    {
        try
        {
            operation();
        }
        catch (FileNotFoundException ex) when (ex.Message.Contains("Could not load file or assembly"))
        {
            string assemblyName = ExtractAssemblyName(ex.Message);
            RecordDependencyError(assemblyName, operationName);
        }
        catch (Exception ex)
        {
            RecordError($"{operationName}失败", ex.Message, "请检查系统环境或重新安装程序");
        }
    }

    /// <summary>
    /// 从异常消息中提取程序集名称
    /// </summary>
    private static string ExtractAssemblyName(string exceptionMessage)
    {
        try
        {
            // 查找 "Could not load file or assembly '" 后面的内容
            var startIndex = exceptionMessage.IndexOf("Could not load file or assembly '");
            if (startIndex >= 0)
            {
                startIndex += "Could not load file or assembly '".Length;
                var endIndex = exceptionMessage.IndexOf("'", startIndex);
                if (endIndex > startIndex)
                {
                    var fullName = exceptionMessage.Substring(startIndex, endIndex - startIndex);
                    // 只返回程序集名称，不包含版本等信息
                    var commaIndex = fullName.IndexOf(',');
                    return commaIndex > 0 ? fullName.Substring(0, commaIndex) : fullName;
                }
            }
        }
        catch
        {
            // 如果解析失败，返回通用错误
        }
        
        return "未知程序集";
    }
}

/// <summary>
/// 简单的内容对话框实现
/// </summary>
public class ContentDialog : Window
{
    public new string Title { get; set; } = "";
    public new object Content { get; set; }
    public string PrimaryButtonText { get; set; } = "确定";
    public string SecondaryButtonText { get; set; } = "";
    public ContentDialogButton DefaultButton { get; set; } = ContentDialogButton.Primary;

    public async Task<ContentDialogResult> ShowAsync(Window parent)
    {
        this.Title = this.Title;
        this.Width = 600;
        this.Height = 400;
        this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        var result = ContentDialogResult.None;
        
        var panel = new StackPanel { Margin = new Avalonia.Thickness(20) };
        
        if (Content != null)
        {
            if (Content is Control control)
            {
                panel.Children.Add(control);
            }
            else
            {
                panel.Children.Add(new TextBlock { Text = Content.ToString(), TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            }
        }
        
        var buttonPanel = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal, 
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 20, 0, 0)
        };
        
        var primaryButton = new Button 
        { 
            Content = PrimaryButtonText, 
            Margin = new Avalonia.Thickness(0, 0, 10, 0),
            MinWidth = 80
        };
        primaryButton.Click += (s, e) => { result = ContentDialogResult.Primary; Close(); };
        buttonPanel.Children.Add(primaryButton);
        
        if (!string.IsNullOrEmpty(SecondaryButtonText))
        {
            var secondaryButton = new Button 
            { 
                Content = SecondaryButtonText,
                MinWidth = 80
            };
            secondaryButton.Click += (s, e) => { result = ContentDialogResult.Secondary; Close(); };
            buttonPanel.Children.Add(secondaryButton);
        }
        
        panel.Children.Add(buttonPanel);
        this.Content = panel;
        
        await ShowDialog(parent);
        return result;
    }
}

public enum ContentDialogButton
{
    Primary,
    Secondary
}

public enum ContentDialogResult
{
    None,
    Primary,
    Secondary
}
