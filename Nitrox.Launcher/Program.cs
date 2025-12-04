using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Svg.Skia;

namespace Nitrox.Launcher;

internal static class Program
{
    // Don't use any Avalonia, third-party APIs or any SynchronizationContext-reliant code before AppMain is called
    // Things aren't initialized yet and stuff might break
    [STAThread]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.Handler;
        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AssemblyResolver.Handler;

        // 执行启动前系统兼容性检查
        if (!args.Contains("--skip-compatibility-check") && !args.Contains("--crash-report"))
        {
            PerformStartupCompatibilityCheck();
        }

        LoadAvalonia(args);
    }
    
    private static void PerformStartupCompatibilityCheck()
    {
        try
        {
            Console.WriteLine("Nitrox 启动器正在检查系统兼容性...");
            
            // 动态加载兼容性检查器以避免在早期阶段的依赖问题
            var checkerType = Type.GetType("Nitrox.Launcher.Models.Utils.SystemCompatibilityChecker, Nitrox.Launcher");
            if (checkerType != null)
            {
                var checkMethod = checkerType.GetMethod("CheckSystemCompatibility");
                var autoFixMethod = checkerType.GetMethod("TryAutoFixIssues");
                
                if (checkMethod != null && autoFixMethod != null)
                {
                    var result = checkMethod.Invoke(null, null);
                    
                    // 调用PrintSummary方法显示检查结果
                    var printSummaryMethod = result.GetType().GetMethod("PrintSummary");
                    printSummaryMethod?.Invoke(result, null);
                    
                    // 检查是否有严重错误
                    var isCompatibleProp = result.GetType().GetProperty("IsCompatible");
                    var errorsProp = result.GetType().GetProperty("Errors");
                    
                    if (isCompatibleProp != null && errorsProp != null)
                    {
                        bool isCompatible = (bool)isCompatibleProp.GetValue(result);
                        var errors = errorsProp.GetValue(result) as System.Collections.IList;
                        
                        if (!isCompatible && errors?.Count > 0)
                        {
                            Console.WriteLine("⚠️ 检测到兼容性问题，正在尝试自动修复...");
                            bool hasFixed = (bool)autoFixMethod.Invoke(null, new[] { result });
                            
                            if (!hasFixed)
                            {
                                Console.WriteLine("❌ 系统兼容性检查失败。启动器可能无法正常运行。");
                                Console.WriteLine("按任意键继续启动（风险自负）或关闭窗口退出...");
                                Console.ReadKey();
                            }
                            else
                            {
                                Console.WriteLine("✅ 自动修复完成，继续启动...");
                            }
                        }
                        else
                        {
                            Console.WriteLine("✅ 系统兼容性检查通过");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"兼容性检查过程中出错: {ex.Message}");
            Console.WriteLine("将跳过兼容性检查，继续启动...");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadAvalonia(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            HandleStartupException(ex);
        }
    }
    
    private static void HandleStartupException(Exception ex)
    {
        // 尝试使用控制台输出，因为日志系统可能还未初始化
        Console.WriteLine($"Nitrox启动器遇到严重错误: {ex.Message}");
        Console.WriteLine("正在执行系统诊断...");
        
        try
        {
            // 尝试使用诊断工具
            var diagnosticType = Type.GetType("Nitrox.Launcher.Models.Utils.LauncherDiagnostics, Nitrox.Launcher");
            if (diagnosticType != null)
            {
                var performDiagnosticMethod = diagnosticType.GetMethod("PerformFullDiagnostic");
                var tryAutoFixMethod = diagnosticType.GetMethod("TryAutoFix");
                
                if (performDiagnosticMethod != null && tryAutoFixMethod != null)
                {
                    var diagnostic = performDiagnosticMethod.Invoke(null, null);
                    bool hasFixed = (bool)tryAutoFixMethod.Invoke(null, new[] { diagnostic });
                    
                    if (hasFixed)
                    {
                        Console.WriteLine("已应用自动修复，正在重试启动...");
                        try
                        {
                            BuildAvaloniaApp().StartWithClassicDesktopLifetime(Environment.GetCommandLineArgs().Skip(1).ToArray());
                            return; // 成功启动
                        }
                        catch
                        {
                            // 修复后仍然失败，继续原有错误处理
                        }
                    }
                }
            }
        }
        catch
        {
            // 诊断工具本身失败，忽略并继续原有错误处理
        }
        
        // 如果诊断和修复都失败，使用原有的错误处理
        App.HandleUnhandledException(ex);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        // https://github.com/wieslawsoltes/Svg.Skia?tab=readme-ov-file#avalonia-previewer
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);

        return App.Create();
    }

    private static class AssemblyResolver
    {
        private static string? currentExecutableDirectory;
        private static readonly Dictionary<string, Assembly> cache = [];
        private static readonly HashSet<string> failedResolves = [];

        public static Assembly? Handler(object sender, ResolveEventArgs args)
        {
            // 避免重复尝试已经失败的程序集
            if (failedResolves.Contains(args.Name))
            {
                return null;
            }

            static Assembly? ResolveFromLib(ReadOnlySpan<char> dllName, string executableDir)
            {
                dllName = dllName.Slice(0, dllName.IndexOf(','));
                if (!dllName.EndsWith(".dll"))
                {
                    dllName = string.Concat(dllName, ".dll");
                }

                if (dllName.EndsWith(".resources.dll"))
                {
                    return null;
                }

                string dllNameStr = dllName.ToString();

                // 扩展搜索路径以解决更多依赖问题
                string[] searchPaths = {
                    Path.Combine(executableDir, "lib", "net472", dllNameStr),
                    Path.Combine(executableDir, "lib", dllNameStr),
                    Path.Combine(executableDir, dllNameStr),
                    Path.Combine(executableDir, "runtimes", "win-x64", "native", dllNameStr),
                    Path.Combine(executableDir, "runtimes", "win-x64", "lib", "net8.0", dllNameStr),
                    Path.Combine(executableDir, "runtimes", "win-x86", "native", dllNameStr),
                    Path.Combine(executableDir, "ref", dllNameStr)
                };

                foreach (string dllPath in searchPaths)
                {
                    try
                    {
                        if (File.Exists(dllPath))
                        {
                            Console.WriteLine($"正在从 {dllPath} 加载程序集");
                            return Assembly.LoadFile(dllPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"从 {dllPath} 加载程序集失败: {ex.Message}");
                    }
                }

                return null;
            }

            if (!cache.TryGetValue(args.Name, out Assembly assembly))
            {
                try
                {
                    string executableDir = GetExecutableDirectory();
                    cache[args.Name] = assembly = ResolveFromLib(args.Name, executableDir);
                    
                    if (assembly == null && !args.Name.Contains(".resources"))
                    {
                        try
                        {
                            // 尝试从GAC或其他标准位置加载
                            cache[args.Name] = assembly = Assembly.Load(args.Name);
                        }
                        catch (Exception loadEx)
                        {
                            Console.WriteLine($"标准加载失败 {args.Name}: {loadEx.Message}");
                            failedResolves.Add(args.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"程序集解析完全失败 {args.Name}: {ex.Message}");
                    failedResolves.Add(args.Name);
                }
            }

            return assembly;
        }

        private static string GetExecutableDirectory()
        {
            if (currentExecutableDirectory != null)
            {
                return currentExecutableDirectory;
            }
            
            try
            {
                string pathAttempt = Assembly.GetEntryAssembly()?.Location;
                if (string.IsNullOrWhiteSpace(pathAttempt))
                {
                    using Process proc = Process.GetCurrentProcess();
                    pathAttempt = proc.MainModule?.FileName;
                }
                
                if (!string.IsNullOrWhiteSpace(pathAttempt))
                {
                    currentExecutableDirectory = new Uri(Path.GetDirectoryName(pathAttempt) ?? Directory.GetCurrentDirectory()).LocalPath;
                }
                else
                {
                    currentExecutableDirectory = Directory.GetCurrentDirectory();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取可执行文件目录失败: {ex.Message}");
                currentExecutableDirectory = Directory.GetCurrentDirectory();
            }
            
            return currentExecutableDirectory;
        }
    }
}
