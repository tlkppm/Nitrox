using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using NitroxModel.Logger;

namespace Nitrox.Launcher.Models.Utils;

/// <summary>
/// 系统兼容性检查器 - 在启动前检查系统是否满足运行要求
/// </summary>
public static class SystemCompatibilityChecker
{
    /// <summary>
    /// 执行启动前的系统兼容性检查
    /// </summary>
    public static CompatibilityCheckResult CheckSystemCompatibility()
    {
        var result = new CompatibilityCheckResult();
        
        Console.WriteLine("正在检查系统兼容性...");
        
        // 检查操作系统版本
        CheckOperatingSystem(result);
        
        // 检查.NET运行时
        CheckDotNetRuntime(result);
        
        // 检查Visual C++运行库
        CheckVcRedistributable(result);
        
        // 检查系统架构
        CheckSystemArchitecture(result);
        
        // 检查磁盘空间
        CheckDiskSpace(result);
        
        // 检查内存
        CheckAvailableMemory(result);
        
        Console.WriteLine($"兼容性检查完成: {(result.IsCompatible ? "通过" : "发现问题")}");
        
        return result;
    }
    
    private static void CheckOperatingSystem(CompatibilityCheckResult result)
    {
        try
        {
            var os = Environment.OSVersion;
            result.SystemInfo["操作系统"] = os.ToString();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows 10 1809 (build 17763) 或更高版本
                if (os.Version.Major >= 10 && os.Version.Build >= 17763)
                {
                    result.CheckResults["操作系统"] = "✅ 兼容";
                }
                else if (os.Version.Major >= 10)
                {
                    result.CheckResults["操作系统"] = "⚠️ 可能兼容（建议更新到最新版本）";
                    result.Warnings.Add("建议将Windows 10更新到1809版本或更高版本以获得最佳兼容性");
                }
                else
                {
                    result.CheckResults["操作系统"] = "❌ 不兼容";
                    result.Errors.Add("需要Windows 10版本1809或更高版本");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                result.CheckResults["操作系统"] = "⚠️ 实验性支持";
                result.Warnings.Add("Linux支持仍在实验阶段，可能遇到兼容性问题");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                result.CheckResults["操作系统"] = "⚠️ 实验性支持";
                result.Warnings.Add("macOS支持仍在实验阶段，可能遇到兼容性问题");
            }
            else
            {
                result.CheckResults["操作系统"] = "❌ 不支持";
                result.Errors.Add("不支持的操作系统");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"无法检查操作系统版本: {ex.Message}");
        }
    }
    
    private static void CheckDotNetRuntime(CompatibilityCheckResult result)
    {
        try
        {
            var runtimeVersion = RuntimeInformation.FrameworkDescription;
            result.SystemInfo[".NET运行时"] = runtimeVersion;
            
            if (runtimeVersion.Contains(".NET 9") || runtimeVersion.Contains(".NET 8") || 
                runtimeVersion.Contains(".NET Core 3.1") || runtimeVersion.Contains(".NET 6") || 
                runtimeVersion.Contains(".NET 7"))
            {
                result.CheckResults[".NET运行时"] = "✅ 兼容";
            }
            else if (runtimeVersion.Contains(".NET Framework"))
            {
                // 检查.NET Framework版本
                var frameworkVersion = Environment.Version;
                if (frameworkVersion.Major >= 4 && frameworkVersion.Minor >= 7)
                {
                    result.CheckResults[".NET运行时"] = "✅ 兼容";
                }
                else
                {
                    result.CheckResults[".NET运行时"] = "❌ 版本过低";
                    result.Errors.Add("需要.NET Framework 4.7.2或更高版本，或者.NET 9/.NET 8运行时");
                }
            }
            else
            {
                result.CheckResults[".NET运行时"] = "⚠️ 未知版本";
                result.Warnings.Add("检测到未知的.NET运行时版本，可能影响兼容性");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"无法检查.NET运行时: {ex.Message}");
        }
    }
    
    [SupportedOSPlatform("windows")]
    private static void CheckVcRedistributable(CompatibilityCheckResult result)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }
        
        try
        {
            bool vcRedist2019Found = false;
            bool vcRedist2015Found = false;
            
            var registryPaths = new[]
            {
                @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\X64"
            };
            
            foreach (var regPath in registryPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(regPath);
                    if (key != null)
                    {
                        var installed = key.GetValue("Installed");
                        var version = key.GetValue("Version")?.ToString();
                        
                        if (installed != null && installed.ToString() == "1")
                        {
                            if (version != null)
                            {
                                var versionParts = version.Split('.');
                                if (versionParts.Length >= 3 && int.TryParse(versionParts[0], out int major))
                                {
                                    if (major >= 14 && int.TryParse(versionParts[1], out int minor))
                                    {
                                        if (minor >= 29) vcRedist2019Found = true;
                                        else vcRedist2015Found = true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // 忽略注册表访问错误
                }
            }
            
            if (vcRedist2019Found)
            {
                result.CheckResults["VC++运行库"] = "✅ 已安装最新版本";
            }
            else if (vcRedist2015Found)
            {
                result.CheckResults["VC++运行库"] = "⚠️ 已安装但建议更新";
                result.Warnings.Add("建议更新到Visual C++ Redistributable 2015-2022最新版本");
            }
            else
            {
                result.CheckResults["VC++运行库"] = "❌ 未安装";
                result.Errors.Add("需要安装Visual C++ Redistributable 2015-2022");
                result.Solutions.Add("下载地址: https://docs.microsoft.com/zh-cn/cpp/windows/latest-supported-vc-redist");
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"无法检查VC++运行库: {ex.Message}");
        }
    }
    
    private static void CheckSystemArchitecture(CompatibilityCheckResult result)
    {
        try
        {
            string architecture = RuntimeInformation.ProcessArchitecture.ToString();
            bool is64BitOS = Environment.Is64BitOperatingSystem;
            bool is64BitProcess = Environment.Is64BitProcess;
            
            result.SystemInfo["系统架构"] = $"{architecture} (OS: {(is64BitOS ? "64位" : "32位")}, Process: {(is64BitProcess ? "64位" : "32位")})";
            
            if (is64BitOS)
            {
                result.CheckResults["系统架构"] = "✅ 64位系统";
            }
            else
            {
                result.CheckResults["系统架构"] = "❌ 32位系统";
                result.Errors.Add("需要64位操作系统");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"无法检查系统架构: {ex.Message}");
        }
    }
    
    private static void CheckDiskSpace(CompatibilityCheckResult result)
    {
        try
        {
            string currentDir = Directory.GetCurrentDirectory();
            var drive = new DriveInfo(Path.GetPathRoot(currentDir));
            
            long availableSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            result.SystemInfo["可用磁盘空间"] = $"{availableSpaceGB} GB";
            
            if (availableSpaceGB >= 5)
            {
                result.CheckResults["磁盘空间"] = "✅ 充足";
            }
            else if (availableSpaceGB >= 2)
            {
                result.CheckResults["磁盘空间"] = "⚠️ 较少";
                result.Warnings.Add("可用磁盘空间较少，建议清理磁盘");
            }
            else
            {
                result.CheckResults["磁盘空间"] = "❌ 不足";
                result.Errors.Add("磁盘空间不足，至少需要2GB可用空间");
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"无法检查磁盘空间: {ex.Message}");
        }
    }
    
    private static void CheckAvailableMemory(CompatibilityCheckResult result)
    {
        try
        {
            long totalMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
            result.SystemInfo["进程内存使用"] = $"{totalMemoryMB} MB";
            
            // 在Windows上尝试获取系统内存信息
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var pc = new PerformanceCounter("Memory", "Available MBytes");
                    float availableMemoryMB = pc.NextValue();
                    result.SystemInfo["可用系统内存"] = $"{availableMemoryMB:F0} MB";
                    
                    if (availableMemoryMB >= 2048)
                    {
                        result.CheckResults["系统内存"] = "✅ 充足";
                    }
                    else if (availableMemoryMB >= 1024)
                    {
                        result.CheckResults["系统内存"] = "⚠️ 较少";
                        result.Warnings.Add("可用内存较少，可能影响性能");
                    }
                    else
                    {
                        result.CheckResults["系统内存"] = "❌ 不足";
                        result.Errors.Add("可用内存不足，建议关闭其他程序");
                    }
                }
                catch
                {
                    result.CheckResults["系统内存"] = "⚠️ 无法检测";
                }
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"无法检查内存信息: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 尝试自动修复发现的问题
    /// </summary>
    public static bool TryAutoFixIssues(CompatibilityCheckResult checkResult)
    {
        bool hasFixed = false;
        
        // 尝试清理临时文件以释放磁盘空间
        if (checkResult.Errors.Any(e => e.Contains("磁盘空间")))
        {
            try
            {
                var tempDir = Path.GetTempPath();
                var nitroxTempFiles = Directory.GetFiles(tempDir, "Nitrox*", SearchOption.TopDirectoryOnly);
                
                foreach (var file in nitroxTempFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // 忽略删除失败的文件
                    }
                }
                
                Console.WriteLine("已清理Nitrox临时文件");
                hasFixed = true;
            }
            catch
            {
                // 忽略清理失败
            }
        }
        
        return hasFixed;
    }
}

/// <summary>
/// 兼容性检查结果
/// </summary>
public class CompatibilityCheckResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> Solutions { get; } = new();
    public Dictionary<string, string> CheckResults { get; } = new();
    public Dictionary<string, string> SystemInfo { get; } = new();
    
    public bool IsCompatible => Errors.Count == 0;
    public bool HasWarnings => Warnings.Count > 0;
    
    public void PrintSummary()
    {
        Console.WriteLine("\n=== 系统兼容性检查报告 ===");
        
        foreach (var check in CheckResults)
        {
            Console.WriteLine($"{check.Key}: {check.Value}");
        }
        
        if (Errors.Count > 0)
        {
            Console.WriteLine("\n❌ 发现的问题:");
            foreach (var error in Errors)
            {
                Console.WriteLine($"  • {error}");
            }
        }
        
        if (Warnings.Count > 0)
        {
            Console.WriteLine("\n⚠️ 警告:");
            foreach (var warning in Warnings)
            {
                Console.WriteLine($"  • {warning}");
            }
        }
        
        if (Solutions.Count > 0)
        {
            Console.WriteLine("\n💡 建议的解决方案:");
            foreach (var solution in Solutions)
            {
                Console.WriteLine($"  • {solution}");
            }
        }
        
        Console.WriteLine();
    }
}
