using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using NitroxModel.Logger;

namespace Nitrox.Launcher.Models.Utils;

/// <summary>
/// 启动器诊断工具 - 用于检测和解决常见的启动问题
/// </summary>
public static class LauncherDiagnostics
{
    /// <summary>
    /// 执行完整的系统诊断
    /// </summary>
    public static DiagnosticResult PerformFullDiagnostic()
    {
        var result = new DiagnosticResult();
        
        Log.Info("开始执行启动器系统诊断...");
        
        // 检查运行时环境
        CheckRuntimeEnvironment(result);
        
        // 检查必需的DLL
        CheckRequiredDlls(result);
        
        // 检查Visual C++ Redistributable
        CheckVcRedist(result);
        
        // 检查.NET Runtime
        CheckDotNetRuntime(result);
        
        // 检查文件权限
        CheckFilePermissions(result);
        
        // 检查Avalonia依赖
        CheckAvaloniaDependencies(result);
        
        Log.Info($"诊断完成。发现 {result.Errors.Count} 个错误，{result.Warnings.Count} 个警告");
        
        return result;
    }
    
    private static void CheckRuntimeEnvironment(DiagnosticResult result)
    {
        try
        {
            result.SystemInfo["OS"] = Environment.OSVersion.ToString();
            result.SystemInfo["64位系统"] = Environment.Is64BitOperatingSystem.ToString();
            result.SystemInfo["64位进程"] = Environment.Is64BitProcess.ToString();
            result.SystemInfo[".NET版本"] = Environment.Version.ToString();
            result.SystemInfo["工作目录"] = Environment.CurrentDirectory;
            
            Log.Info($"系统信息: {Environment.OSVersion}, 64位: {Environment.Is64BitOperatingSystem}");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"无法获取系统信息: {ex.Message}");
        }
    }
    
    private static void CheckRequiredDlls(DiagnosticResult result)
    {
        var requiredDlls = new[]
        {
            "kernel32.dll",
            "KERNELBASE.dll", 
            "user32.dll",
            "msvcp140.dll",
            "vcruntime140.dll",
            "api-ms-win-crt-runtime-l1-1-0.dll"
        };
        
        foreach (var dll in requiredDlls)
        {
            try
            {
                IntPtr handle = LoadLibrary(dll);
                if (handle != IntPtr.Zero)
                {
                    FreeLibrary(handle);
                    result.SystemInfo[$"DLL_{dll}"] = "可用";
                }
                else
                {
                    result.Errors.Add($"无法加载必需的DLL: {dll}");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"检查DLL {dll} 时出错: {ex.Message}");
            }
        }
    }
    
    [SupportedOSPlatform("windows")]
    private static void CheckVcRedist(DiagnosticResult result)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }
        
        try
        {
            var vcRedistVersions = new[]
            {
                @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64",
                @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X86",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\X64",
                @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\X86"
            };
            
            bool vcRedistFound = false;
            
            foreach (var regPath in vcRedistVersions)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(regPath);
                    if (key != null)
                    {
                        var installed = key.GetValue("Installed");
                        var version = key.GetValue("Version");
                        
                        if (installed != null && installed.ToString() == "1")
                        {
                            vcRedistFound = true;
                            result.SystemInfo[$"VC++运行库_{regPath.Split('\\').Last()}"] = version?.ToString() ?? "已安装";
                        }
                    }
                }
                catch
                {
                    // 忽略注册表访问错误
                }
            }
            
            if (!vcRedistFound)
            {
                result.Errors.Add("未找到Visual C++ Redistributable 2015-2022，这可能导致KERNELBASE.dll初始化失败");
                result.Solutions.Add("请下载并安装最新的Visual C++ Redistributable 2015-2022");
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"无法检查VC++运行库: {ex.Message}");
        }
    }
    
    private static void CheckDotNetRuntime(DiagnosticResult result)
    {
        try
        {
            var runtimeVersion = RuntimeInformation.FrameworkDescription;
            result.SystemInfo[".NET运行时"] = runtimeVersion;
            
            if (!runtimeVersion.Contains(".NET 8") && !runtimeVersion.Contains(".NET Core"))
            {
                result.Warnings.Add($"当前.NET运行时: {runtimeVersion}，建议使用.NET 8");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"无法获取.NET运行时信息: {ex.Message}");
        }
    }
    
    private static void CheckFilePermissions(DiagnosticResult result)
    {
        try
        {
            string executablePath = Assembly.GetEntryAssembly()?.Location ?? Environment.ProcessPath;
            if (executablePath != null)
            {
                string executableDir = Path.GetDirectoryName(executablePath);
                
                // 测试文件读写权限
                string testFile = Path.Combine(executableDir, "test_permissions.tmp");
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    result.SystemInfo["文件权限"] = "正常";
                }
                catch
                {
                    result.Errors.Add("启动器目录没有写入权限，请以管理员身份运行或更改文件夹权限");
                }
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"无法检查文件权限: {ex.Message}");
        }
    }
    
    private static void CheckAvaloniaDependencies(DiagnosticResult result)
    {
        try
        {
            // 检查Avalonia相关程序集
            var avaloniaAssemblies = new[]
            {
                "Avalonia.Base",
                "Avalonia.Controls",
                "Avalonia.Markup.Xaml",
                "Avalonia.Svg.Skia"
            };
            
            foreach (var assemblyName in avaloniaAssemblies)
            {
                try
                {
                    Assembly.Load(assemblyName);
                    result.SystemInfo[$"Avalonia_{assemblyName}"] = "已加载";
                }
                catch
                {
                    result.Warnings.Add($"无法加载Avalonia程序集: {assemblyName}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"检查Avalonia依赖时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 应用常见问题的自动修复
    /// </summary>
    public static bool TryAutoFix(DiagnosticResult diagnostic)
    {
        bool hasFixed = false;
        
        // 尝试修复程序集解析问题
        if (diagnostic.Errors.Any(e => e.Contains("DLL") || e.Contains("程序集")))
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += FallbackAssemblyResolver;
                hasFixed = true;
                Log.Info("已应用备用程序集解析器");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "应用备用程序集解析器失败");
            }
        }
        
        return hasFixed;
    }
    
    private static Assembly FallbackAssemblyResolver(object sender, ResolveEventArgs args)
    {
        try
        {
            string assemblyName = args.Name.Split(',')[0];
            string executableDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Environment.ProcessPath);
            
            if (executableDir != null)
            {
                string[] searchPaths = {
                    executableDir,
                    Path.Combine(executableDir, "lib"),
                    Path.Combine(executableDir, "runtimes", "win-x64", "native"),
                    Path.Combine(executableDir, "runtimes", "win-x64", "lib", "net8.0")
                };
                
                foreach (string searchPath in searchPaths)
                {
                    string assemblyPath = Path.Combine(searchPath, assemblyName + ".dll");
                    if (File.Exists(assemblyPath))
                    {
                        return Assembly.LoadFrom(assemblyPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"备用程序集解析器无法解析: {args.Name}");
        }
        
        return null;
    }
    
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);
    
    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);
}

/// <summary>
/// 诊断结果
/// </summary>
public class DiagnosticResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> Solutions { get; } = new();
    public Dictionary<string, string> SystemInfo { get; } = new();
    
    public bool HasCriticalErrors => Errors.Count > 0;
    public bool IsHealthy => Errors.Count == 0 && Warnings.Count == 0;
}
