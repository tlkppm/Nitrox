using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NitroxModel;
using NitroxModel.Core;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using NitroxModel.Helper;
using NitroxServer;
using NitroxServer.ConsoleCommands.Processor;
using NitroxServer.GameLogic;

namespace Nitrox.Server.Subnautica;

[SuppressMessage("Usage", "DIMA001:Dependency Injection container is used directly")]
public class Program
{
    private static Lazy<string> gameInstallDir;
    private static readonly CircularBuffer<string> inputHistory = new(1000);
    private static int currentHistoryIndex;
    private static readonly CancellationTokenSource serverCts = new();
    private static Ipc.ServerIpc ipc;

    // 新增：Generic Host相关
    private static bool useGenericHost = false; // 默认使用旧系统，可通过参数启用新系统

    private static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.Handler;
        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AssemblyResolver.Handler;

        // DEBUG: 确认运行的是修改版本
        Console.WriteLine("[DEBUG] 运行修改版服务端 - 支持双模式启动");
        
        // 智能检查是否启用Generic Host
        useGenericHost = ShouldUseGenericHost(args);

        if (useGenericHost)
        {
            Console.WriteLine("[DEBUG] 尝试使用新服务端模式 (.NET Generic Host)");
            try
            {
                await StartServerWithGenericHostAsync(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] 新服务端启动失败，自动切换到传统模式: {ex.Message}");
                
                // 等待一点时间让资源释放
                Console.WriteLine("[DEBUG] 等待资源释放...");
                await Task.Delay(2000);
                
                await StartServer(args);
            }
        }
        else
        {
            Console.WriteLine("[DEBUG] 使用传统服务端模式");
            await StartServer(args);
        }
    }

    /// <summary>
    /// 智能判断是否应该使用Generic Host模式
    /// </summary>
    private static bool ShouldUseGenericHost(string[] args)
    {
        // 调试：输出所有命令行参数（使用Console确保立即显示）
        Console.WriteLine($"[DEBUG] 检测到的命令行参数: [{string.Join(", ", args)}]");
        Console.WriteLine($"[DEBUG] 参数数量: {args.Length}");
        
        // 1. 优先级最高：命令行参数
        if (args.Contains("--use-generic-host", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("[DEBUG] 通过命令行参数启用新服务端模式");
            return true;
        }
        if (args.Contains("--use-legacy", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("[DEBUG] 通过命令行参数强制使用传统模式");
            return false;
        }

        // 2. 检查服务器配置文件
        try
        {
            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.cfg");
            if (File.Exists(configFile))
            {
                var lines = File.ReadAllLines(configFile);
                var useGenericHostLine = lines.FirstOrDefault(l => l.StartsWith("UseGenericHost=", StringComparison.OrdinalIgnoreCase));
                if (useGenericHostLine != null)
                {
                    var value = useGenericHostLine.Split('=')[1].Trim();
                    if (bool.TryParse(value, out bool result))
                    {
                        Log.Info($"从配置文件读取服务端模式设置: {result}");
                        return result;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Info($"读取配置文件时出错: {ex.Message}");
        }

        // 3. 检查环境变量（开发环境）
        var environment = Environment.GetEnvironmentVariable("NITROX_ENVIRONMENT");
        Console.WriteLine($"[DEBUG] 环境变量 NITROX_ENVIRONMENT: {environment ?? "未设置"}");
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[DEBUG] 开发环境默认启用新服务端模式");
            Log.Info("开发环境默认启用新服务端模式");
            return true;
        }

        // 4. 检查是否存在appsettings.json（表示用户想使用新功能）
        // 注意：只有在明确配置了Generic Host时才启用，避免意外的自动切换
        var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        Console.WriteLine($"[DEBUG] 检查appsettings.json路径: {appSettingsPath}");
        Console.WriteLine($"[DEBUG] appsettings.json是否存在: {File.Exists(appSettingsPath)}");
        if (File.Exists(appSettingsPath))
        {
            try
            {
                var content = File.ReadAllText(appSettingsPath);
                Console.WriteLine($"[DEBUG] appsettings.json内容: {content}");
                // 只有当appsettings.json明确包含Generic Host配置时才启用
                if (content.Contains("\"UseGenericHost\"") && content.Contains("true"))
                {
                    Console.WriteLine("[DEBUG] appsettings.json包含UseGenericHost=true，启用新服务端模式");
                    Log.Info("检测到appsettings.json中的Generic Host配置，启用新服务端模式");
                    return true;
                }
                else
                {
                    Console.WriteLine("[DEBUG] appsettings.json存在但未配置UseGenericHost=true");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] 读取appsettings.json时出错: {ex.Message}");
            }
        }

        // 5. 默认使用传统模式（安全选择）
        Console.WriteLine("[DEBUG] 所有检查完成，默认使用传统服务端模式");
        Log.Info("默认使用传统服务端模式");
        return false;
    }

    /// <summary>
    /// 新的Generic Host启动方式
    /// </summary>
    private static async Task StartServerWithGenericHostAsync(string[] args)
    {
        Console.WriteLine("[DEBUG] Generic Host模式启动开始");
        Log.Info("Generic Host模式启动");
        
        try
        {
            // 🔧 创建IPC服务器实例以支持启动器进程监控
            Console.WriteLine("[DEBUG] 创建IPC服务器实例");
            ipc = new Ipc.ServerIpc(Environment.ProcessId, CancellationTokenSource.CreateLinkedTokenSource(serverCts.Token));
            bool isConsoleApp = !args.Contains("--embedded", StringComparer.OrdinalIgnoreCase);
            Log.Setup(
                asyncConsoleWriter: true,
                isConsoleApp: isConsoleApp,
                logOutputCallback: isConsoleApp ? null : msg => _ = ipc.SendOutput(msg)
            );
            Console.WriteLine("[DEBUG] IPC服务器创建完成");
            
            // 🔧 关键修复：在初始化DI容器之前，先设置游戏目录
            // 这确保ResourceAssetsParser能够找到Assembly-CSharp等游戏程序集
            Console.WriteLine("[DEBUG] 开始设置游戏目录");
            string gameDir;
            if (args.Length > 0 && Directory.Exists(args[0]) && File.Exists(Path.Combine(args[0], GameInfo.Subnautica.ExeName)))
            {
                gameDir = Path.GetFullPath(args[0]);
                gameInstallDir = new Lazy<string>(() => gameDir);
            }
            else
            {
                gameInstallDir = new Lazy<string>(() =>
                {
                    return gameDir = NitroxUser.GamePath;
                });
            }
            Console.WriteLine($"[DEBUG] 设置游戏目录完成: {gameInstallDir.Value}");
            Log.Info($"Using game files from: \'{gameInstallDir.Value}\'");
            
            // 现在可以安全地初始化DI容器，ResourceAssetsParser能够找到游戏程序集
            Console.WriteLine("[DEBUG] 开始初始化DI容器");
            NitroxServiceLocator.InitializeDependencyContainer(new SubnauticaServerAutoFacRegistrar());
            Console.WriteLine("[DEBUG] DI容器初始化完成");
            
            Console.WriteLine("[DEBUG] 开始新的生命周期范围");
            NitroxServiceLocator.BeginNewLifetimeScope();
            Console.WriteLine("[DEBUG] 生命周期范围创建完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Generic Host初始化过程中出错: {ex.Message}");
            Console.WriteLine($"[DEBUG] 异常详细信息: {ex}");
            throw;
        }
        
        try
        {
            // 获取并启动服务器
            Console.WriteLine("[DEBUG] 开始获取服务器实例");
            var server = NitroxServiceLocator.LocateService<NitroxServer.Server>();
            Console.WriteLine("[DEBUG] 服务器实例获取完成");
            
            Console.WriteLine("[DEBUG] 开始解析服务器保存名称");
            // 🔧 设置IPC玩家数量变更通知，确保启动器能监控服务器状态
            server.PlayerCountChanged += count =>
            {
                _ = ipc.SendOutput($"{Ipc.Messages.PlayerCountMessage}:[{count}]");
            };
            
            var serverSaveName = NitroxServer.Server.GetSaveName(args, "My World");
            Console.WriteLine($"[DEBUG] 服务器保存名称: {serverSaveName}");
            
            Log.Info("使用Generic Host包装启动现有服务器");
            
            // 等待端口可用
            Console.WriteLine($"[DEBUG] 开始等待端口 {server.Port} 可用");
            await WaitForAvailablePortAsync(server.Port, TimeSpan.FromSeconds(30), serverCts.Token);
            Console.WriteLine($"[DEBUG] 端口 {server.Port} 现在可用");
            
            Console.WriteLine("[DEBUG] 开始启动服务器");
            if (!server.Start(serverSaveName, serverCts))
            {
                Console.WriteLine("[DEBUG] 服务器启动失败 - Start方法返回false");
                throw new Exception("服务器启动失败");
            }
            Console.WriteLine("[DEBUG] 服务器启动成功");
            
            Log.Info("Generic Host模式服务器启动成功");
            
            // 输出网络连接信息（与旧版服务端保持一致）
            Console.WriteLine("[DEBUG] 输出网络连接信息");
            try
            {
                Log.Info($"服务器正在监听端口 {server.Port} UDP");
                Log.Info($"最大玩家数: {NitroxServiceLocator.LocateService<NitroxModel.Serialization.SubnauticaServerConfig>().MaxConnections}");
                
                // 显示连接IP信息
                string localIp = "";
                try 
                {
                    var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                                   ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                    foreach (var networkInterface in networkInterfaces)
                    {
                        var ipProps = networkInterface.GetIPProperties();
                        foreach (var addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                var ip = addr.Address.ToString();
                                if (ip.StartsWith("192.168.") || ip.StartsWith("10.") || ip.StartsWith("172."))
                                {
                                    localIp = ip;
                                    break;
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(localIp)) break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] 获取本地IP时出错: {ex.Message}");
                }
                
                Console.WriteLine($"Use IP to connect (端口: {server.Port}):");
                Console.WriteLine($"  127.0.0.1:{server.Port} - You (Local)");
                if (!string.IsNullOrEmpty(localIp))
                {
                    Console.WriteLine($"  {localIp}:{server.Port} - Friends on same internet network (LAN)");
                }
                
                Log.Info($"Use IP to connect (端口: {server.Port}):");
                Log.Info($"  127.0.0.1:{server.Port} - You (Local)");
                if (!string.IsNullOrEmpty(localIp))
                {
                    Log.Info($"  {localIp}:{server.Port} - Friends on same internet network (LAN)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] 获取网络信息时出错: {ex.Message}");
            }
            
            // 输出存档详细信息（与旧版服务端保持一致）
            Console.WriteLine("[DEBUG] 输出存档详细信息");
            try
            {
                var saveSummary = server.GetSaveSummary();
                Console.WriteLine($"[INFO] 已加载存档{saveSummary}");
                Log.Info($"已加载存档{saveSummary}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] 获取存档信息时出错: {ex.Message}");
            }
            
            // 输出服务器状态信息
            Console.WriteLine("[DEBUG] 输出服务器状态信息");
            Log.Info("服务器已启动并等待玩家连接");
            Log.Info("注意: 服务器已暂停，当第一个玩家连接后将自动恢复");
            Console.WriteLine("服务器已启动并等待玩家连接");
            Console.WriteLine("注意: 服务器已暂停，当第一个玩家连接后将自动恢复");
            
            //  新增：启动 Web API 服务用于启动器查询玩家信息
            Console.WriteLine("[DEBUG] 开始启动 Web API 服务");
            var apiHost = await StartWebApiHostAsync(serverCts.Token);
            if (apiHost != null)
            {
                Log.Info($"Web API 已启动，监听端口: {server.Port + 1000}");
                Console.WriteLine($"[API] Web API 已启动在 http://localhost:{server.Port + 1000}");
                Console.WriteLine($"[API] 玩家列表端点: http://localhost:{server.Port + 1000}/api/players");
            }
            else
            {
                Log.Warn("Web API 启动失败，启动器可能无法获取玩家列表");
            }
            
            //  关键修复：添加网络事件轮询机制
            Console.WriteLine("[DEBUG] 进入网络事件轮询状态，服务器正在运行");
            Log.Info("开始网络事件轮询，等待客户端连接");
            
            try
            {
                // 获取网络服务器实例以进行事件轮询
                var networkServer = NitroxServiceLocator.LocateService<NitroxServer.Communication.NitroxServer>();
                
                // 持续轮询网络事件，直到收到取消信号
                while (!serverCts.Token.IsCancellationRequested)
                {
                    // 轮询网络事件 - 这是处理连接和数据包的关键
                    if (networkServer is NitroxServer.Communication.LiteNetLib.LiteNetLibServer liteNetLibServer)
                    {
                        liteNetLibServer.PollNetworkEvents(); // 轮询网络事件
                        
                        // 定期输出详细连接状态（每10秒）
                        if (DateTime.Now.Second % 10 == 0 && DateTime.Now.Millisecond < 50)
                        {
                            int connectedCount = liteNetLibServer.GetConnectedPeersCount();
                            
                            // 获取已连接的玩家信息
                            var playerManager = NitroxServiceLocator.LocateService<PlayerManager>();
                            var connectedPlayers = playerManager.GetConnectedPlayers();
                            
                            if (connectedCount > 0)
                            {
                                Console.WriteLine($"[NETWORK] 当前网络连接数: {connectedCount}");
                                Console.WriteLine($"[NETWORK] 已认证玩家数: {connectedPlayers.Count}");
                                
                                foreach (var player in connectedPlayers)
                                {
                                    string endpoint = player.Connection?.Endpoint?.ToString() ?? "未知IP";
                                    Console.WriteLine($"[NETWORK] → 玩家: {player.Name} | IP: {endpoint} | ID: {player.Id}");
                                }
                            }
                            else
                            {
                                // 每30秒输出一次等待信息
                                if (DateTime.Now.Second % 30 == 0)
                                {
                                    Console.WriteLine($"[NETWORK] 等待玩家连接... | 服务端时间: {DateTime.Now:HH:mm:ss}");
                                }
                            }
                        }
                    }
                    
                    // 短暂休眠避免过度占用CPU
                    await Task.Delay(15, serverCts.Token); // 15ms轮询间隔，与UpdateTime一致
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[DEBUG] 收到停止信号");
                Log.Info("收到停止信号，正在关闭服务器");
            }
            finally
            {
                Console.WriteLine("[DEBUG] 开始停止服务器");
                server.Stop(true);
                Console.WriteLine("[DEBUG] 服务器已停止");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Generic Host启动过程中出错: {ex.Message}");
            Console.WriteLine($"[DEBUG] 异常详细信息: {ex}");
            throw;
        }
    }

    /// <summary>
    /// 现有的启动方式（保持不变）
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task StartServer(string[] args)
    {
        // 现有的启动逻辑保持完全不变
        ipc = new Ipc.ServerIpc(Environment.ProcessId, CancellationTokenSource.CreateLinkedTokenSource(serverCts.Token));
        bool isConsoleApp = !args.Contains("--embedded", StringComparer.OrdinalIgnoreCase);
        Log.Setup(
            asyncConsoleWriter: true,
            isConsoleApp: isConsoleApp,
            logOutputCallback: isConsoleApp ? null : msg => _ = ipc.SendOutput(msg)
        );

        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, CloseWindowHandler);
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, CloseWindowHandler);
        PosixSignalRegistration.Create(PosixSignal.SIGINT, CloseWindowHandler);
        PosixSignalRegistration.Create(PosixSignal.SIGHUP, CloseWindowHandler);

        CultureManager.ConfigureCultureInfo();
        if (!Console.IsInputRedirected)
        {
            Console.TreatControlCAsInput = true;
        }

        Log.Info($"Starting NitroxServer V{NitroxEnvironment.Version} for {GameInfo.Subnautica.FullName}");
        Log.Debug($@"Process start args: ""{string.Join(@""", """, Environment.GetCommandLineArgs())}""");

        Task handleConsoleInputTask;
        NitroxServer.Server server;
        try
        {
            handleConsoleInputTask = HandleConsoleInputAsync(ConsoleCommandHandler(), serverCts.Token);
            AppMutex.Hold(() => Log.Info("Waiting on other Nitrox servers to initialize before starting.."), serverCts.Token);

            Stopwatch watch = Stopwatch.StartNew();

            // Allow game path to be given as command argument
            string gameDir;
            if (args.Length > 0 && Directory.Exists(args[0]) && File.Exists(Path.Combine(args[0], GameInfo.Subnautica.ExeName)))
            {
                gameDir = Path.GetFullPath(args[0]);
                gameInstallDir = new Lazy<string>(() => gameDir);
            }
            else
            {
                gameInstallDir = new Lazy<string>(() =>
                {
                    return gameDir = NitroxUser.GamePath;
                });
            }
            Log.Info($"Using game files from: \'{gameInstallDir.Value}\'");

            // TODO: Fix DI to not be slow (should not use IO in type constructors). Instead, use Lazy<T> (et al). This way, cancellation can be faster.
            Console.WriteLine("[DEBUG] 旧版服务端 - 开始初始化DI容器");
            NitroxServiceLocator.InitializeDependencyContainer(new SubnauticaServerAutoFacRegistrar());
            Console.WriteLine("[DEBUG] 旧版服务端 - DI容器初始化完成");
            
            Console.WriteLine("[DEBUG] 旧版服务端 - 开始新的生命周期范围");
            NitroxServiceLocator.BeginNewLifetimeScope();
            Console.WriteLine("[DEBUG] 旧版服务端 - 生命周期范围创建完成");
            
            Console.WriteLine("[DEBUG] 旧版服务端 - 开始获取Server服务");
            server = NitroxServiceLocator.LocateService<NitroxServer.Server>();
            Console.WriteLine("[DEBUG] 旧版服务端 - Server服务获取完成");
            server.PlayerCountChanged += count =>
            {
                _ = ipc.SendOutput($"{Ipc.Messages.PlayerCountMessage}:[{count}]");
            };
            string serverSaveName = NitroxServer.Server.GetSaveName(args, "My World");
            Log.SaveName = serverSaveName;

            using (CancellationTokenSource portWaitCts = CancellationTokenSource.CreateLinkedTokenSource(serverCts.Token))
            {
                TimeSpan portWaitTimeout = TimeSpan.FromSeconds(30);
                portWaitCts.CancelAfter(portWaitTimeout);
                await WaitForAvailablePortAsync(server.Port, portWaitTimeout, portWaitCts.Token);
            }

            if (!serverCts.IsCancellationRequested)
            {
                if (!server.Start(serverSaveName, serverCts))
                {
                    throw new Exception("Unable to start server.");
                }
                else
                {
                    Log.Info($"Server started ({Math.Round(watch.Elapsed.TotalSeconds, 1)}s)");
                    Log.Info("To get help for commands, run help in console or /help in chatbox");
                }
            }
        }
        finally
        {
            // Allow other servers to start initializing.
            AppMutex.Release();
        }

        await handleConsoleInputTask;
        server.Stop(true);
        ipc.Dispose();

        try
        {
            if (Environment.UserInteractive && Console.In != StreamReader.Null && Debugger.IsAttached)
            {
                Task.Delay(100).Wait(); // Wait for async logs to flush to console
                Console.WriteLine($"{Environment.NewLine}Press any key to continue . . .");
                Console.ReadKey(true);
            }
        }
        catch
        {
            // ignored
        }

        Action<string> ConsoleCommandHandler()
        {
            ConsoleCommandProcessor commandProcessor = null;
            return submit =>
            {
                if (submit == Ipc.Messages.SaveNameMessage)
                {
                    _ = ipc.SendOutput($"{Ipc.Messages.SaveNameMessage}:{Log.SaveName}");
                    return;
                }
                try
                {
                    commandProcessor ??= NitroxServiceLocator.LocateService<ConsoleCommandProcessor>();
                }
                catch (Exception)
                {
                    // ignored
                }
                commandProcessor?.ProcessCommand(submit, Optional.Empty, Perms.CONSOLE);
            };
        }
    }

    private static void CloseWindowHandler(PosixSignalContext context)
    {
        context.Cancel = false;
        serverCts?.Cancel();
    }

    // 现有的所有辅助方法保持不变...
    
    /// <summary>
    /// 现有的控制台输入处理逻辑
    /// </summary>
    private static async Task HandleConsoleInputAsync(Action<string> submitHandler, CancellationToken ct = default)
    {
        // 保持原有的完整实现...
        ConcurrentQueue<string> commandQueue = new();

        if (Console.IsInputRedirected)
        {
            Log.Info("Server input stream is redirected");
            _ = Task.Run(() =>
            {
                while (!ct.IsCancellationRequested)
                {
                    string commandRead = Console.ReadLine();
                    commandQueue.Enqueue(commandRead);
                }
            }, ct).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Log.Error(t.Exception);
                }
            }, ct);
        }
        else
        {
            Log.Info("Server input stream is available");
            StringBuilder inputLineBuilder = new();

            void ClearInputLine()
            {
                currentHistoryIndex = 0;
                inputLineBuilder.Clear();
                Console.Write($"\r{new string(' ', Console.WindowWidth - 1)}\r");
            }

            void RedrawInput(int start = 0, int end = 0)
            {
                int lastPosition = Console.CursorLeft;
                // Expand range to end if end value is -1
                if (start > -1 && end == -1)
                {
                    end = Math.Max(inputLineBuilder.Length - start, 0);
                }

                if (start == 0 && end == 0)
                {
                    // Redraw entire line
                    Console.Write($"\r{new string(' ', Console.WindowWidth - 1)}\r{inputLineBuilder}");
                }
                else
                {
                    // Redraw part of line
                    string changedInputSegment = inputLineBuilder.ToString(start, end);
                    Console.CursorVisible = false;
                    Console.Write($"{changedInputSegment}{new string(' ', inputLineBuilder.Length - changedInputSegment.Length - Console.CursorLeft + 1)}");
                    Console.CursorVisible = true;
                }
                Console.CursorLeft = lastPosition;
            }

            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    if (!Console.KeyAvailable)
                    {
                        try
                        {
                            await Task.Delay(10, ct);
                        }
                        catch (TaskCanceledException)
                        {
                            // ignored
                        }
                        continue;
                    }

                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    // Handle (ctrl) hotkeys
                    if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
                    {
                        switch (keyInfo.Key)
                        {
                            case ConsoleKey.C:
                                if (inputLineBuilder.Length > 0)
                                {
                                    ClearInputLine();
                                    continue;
                                }

                                await serverCts.CancelAsync();
                                return;
                            case ConsoleKey.D:
                                await serverCts.CancelAsync();
                                return;
                            default:
                                // Unhandled modifier key
                                continue;
                        }
                    }

                    if (keyInfo.Modifiers == 0)
                    {
                        switch (keyInfo.Key)
                        {
                            case ConsoleKey.LeftArrow when Console.CursorLeft > 0:
                                Console.CursorLeft--;
                                continue;
                            case ConsoleKey.RightArrow when Console.CursorLeft < inputLineBuilder.Length:
                                Console.CursorLeft++;
                                continue;
                            case ConsoleKey.Backspace:
                                if (inputLineBuilder.Length > Console.CursorLeft - 1 && Console.CursorLeft > 0)
                                {
                                    inputLineBuilder.Remove(Console.CursorLeft - 1, 1);
                                    Console.CursorLeft--;
                                    Console.Write(' ');
                                    Console.CursorLeft--;
                                    RedrawInput();
                                }
                                continue;
                            case ConsoleKey.Delete:
                                if (inputLineBuilder.Length > 0 && Console.CursorLeft < inputLineBuilder.Length)
                                {
                                    inputLineBuilder.Remove(Console.CursorLeft, 1);
                                    RedrawInput(Console.CursorLeft, inputLineBuilder.Length - Console.CursorLeft);
                                }
                                continue;
                            case ConsoleKey.Home:
                                Console.CursorLeft = 0;
                                continue;
                            case ConsoleKey.End:
                                Console.CursorLeft = inputLineBuilder.Length;
                                continue;
                            case ConsoleKey.Escape:
                                ClearInputLine();
                                continue;
                            case ConsoleKey.Tab:
                                if (Console.CursorLeft + 4 < Console.WindowWidth)
                                {
                                    inputLineBuilder.Insert(Console.CursorLeft, "    ");
                                    RedrawInput(Console.CursorLeft, -1);
                                    Console.CursorLeft += 4;
                                }
                                continue;
                            case ConsoleKey.UpArrow when inputHistory.Count > 0 && currentHistoryIndex > -inputHistory.Count:
                                inputLineBuilder.Clear();
                                inputLineBuilder.Append(inputHistory[--currentHistoryIndex]);
                                RedrawInput();
                                Console.CursorLeft = Math.Min(inputLineBuilder.Length, Console.WindowWidth);
                                continue;
                            case ConsoleKey.DownArrow when inputHistory.Count > 0 && currentHistoryIndex < 0:
                                if (currentHistoryIndex == -1)
                                {
                                    ClearInputLine();
                                    continue;
                                }
                                inputLineBuilder.Clear();
                                inputLineBuilder.Append(inputHistory[++currentHistoryIndex]);
                                RedrawInput();
                                Console.CursorLeft = Math.Min(inputLineBuilder.Length, Console.WindowWidth);
                                continue;
                        }
                    }
                    // Handle input submit to submit handler
                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        string submit = inputLineBuilder.ToString();
                        if (inputHistory.Count == 0 || inputHistory[inputHistory.LastChangedIndex] != submit)
                        {
                            inputHistory.Add(submit);
                        }
                        currentHistoryIndex = 0;
                        commandQueue.Enqueue(submit);
                        inputLineBuilder.Clear();
                        Console.WriteLine();
                        continue;
                    }

                    // If unhandled key, append as input.
                    if (keyInfo.KeyChar != 0)
                    {
                        Console.Write(keyInfo.KeyChar);
                        if (Console.CursorLeft - 1 < inputLineBuilder.Length)
                        {
                            inputLineBuilder.Insert(Console.CursorLeft - 1, keyInfo.KeyChar);
                            RedrawInput(Console.CursorLeft, -1);
                        }
                        else
                        {
                            inputLineBuilder.Append(keyInfo.KeyChar);
                        }
                    }
                }
            }, ct).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Log.Error(t.Exception);
                }
            }, ct);
        }

        ipc.StartReadingCommands(command => commandQueue.Enqueue(command), ct);
        
        if (!Console.IsInputRedirected)
        {
            // Important to not hang process: keep command handler on the main thread when input not redirected (i.e. don't Task.Run)
            while (!ct.IsCancellationRequested)
            {
                while (commandQueue.TryDequeue(out string command))
                {
                    submitHandler(command);
                }
                try
                {
                    await Task.Delay(10, ct);
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
            }
        }
        else
        {
            // Important to not hang process (when running launcher from release exe): free main thread if input redirected
            await Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    while (commandQueue.TryDequeue(out string command))
                    {
                        submitHandler(command);
                    }
                    try
                    {
                        await Task.Delay(10, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        // ignored
                    }
                }
            }, ct).ContinueWithHandleError();
        }
    }

    private static async Task WaitForAvailablePortAsync(int port, TimeSpan timeout = default, CancellationToken ct = default)
    {
        if (timeout == default)
        {
            timeout = TimeSpan.FromSeconds(30);
        }
        else
        {
            Validate.IsTrue(timeout.TotalSeconds >= 5, "Timeout must be at least 5 seconds.");
        }

        int messageLength = 0;
        void PrintPortWarn(TimeSpan timeRemaining)
        {
            string message = $"Port {port} UDP is already in use. Please change the server port or close out any program that may be using it. Retrying for {Math.Floor(timeRemaining.TotalSeconds)} seconds until it is available...";
            messageLength = message.Length;
            Log.Warn(message);
        }

        DateTimeOffset time = DateTimeOffset.UtcNow;
        bool first = true;
        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                IPEndPoint endPoint = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().FirstOrDefault(ip => ip.Port == port);
                if (endPoint == null)
                {
                    break;
                }

                if (first)
                {
                    first = false;
                    PrintPortWarn(timeout);
                }
                else if (Environment.UserInteractive && !Console.IsInputRedirected && Console.In != StreamReader.Null)
                {
                    // If not first time, move cursor up the number of lines it takes up to overwrite previous message
                    int numberOfLines = (int)Math.Ceiling( ((double)messageLength + 15) / Console.BufferWidth );
                    for (int i = 0; i < numberOfLines; i++)
                    {
                        if (Console.CursorTop > 0) // Check to ensure we don't go out of bounds
                        {
                            Console.CursorTop--;
                        }
                    }
                    Console.CursorLeft = 0;

                    PrintPortWarn(timeout - (DateTimeOffset.UtcNow - time));
                }

                await Task.Delay(500, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Error(ex);
        }
        if (!Environment.UserInteractive || Console.IsInputRedirected || Console.In == StreamReader.Null)
        {
            return;
        }

        // TODO: Implement log file opening by server name
        /*string mostRecentLogFile = Log.GetMostRecentLogFile(); // Log.SaveName
        if (mostRecentLogFile == null)
        {
            return;
        }

        Log.Info("Press L to open log file before closing. Press any other key to close . . .");*/
        Log.Info("Press L to open log folder before closing. Press any other key to close . . .");
        ConsoleKeyInfo key = Console.ReadKey(true);

        if (key.Key == ConsoleKey.L)
        {
            // Log.Info($"Opening log file at: {mostRecentLogFile}..");
            // using Process process = FileSystem.Instance.OpenOrExecuteFile(mostRecentLogFile);

            Process.Start(new ProcessStartInfo
            {
                FileName = Log.LogDirectory,
                Verb = "open",
                UseShellExecute = true
            })?.Dispose();
        }

        Environment.Exit(1);
    }

    private static class AssemblyResolver
    {
        private static string currentExecutableDirectory;
        private static readonly Dictionary<string, Assembly> resolvedAssemblyCache = [];

        public static Assembly Handler(object sender, ResolveEventArgs args)
        {
            static Assembly ResolveFromLib(ReadOnlySpan<char> dllName)
            {
                dllName = dllName.Slice(0, Math.Max(dllName.IndexOf(','), 0));
                if (dllName.IsEmpty)
                {
                    return null;
                }
                if (!dllName.EndsWith(".dll"))
                {
                    dllName = string.Concat(dllName, ".dll");
                }
                if (dllName.EndsWith(".resources.dll"))
                {
                    return null;
                }
                string dllNameStr = dllName.ToString();
                // If available, return cached assembly
                if (resolvedAssemblyCache.TryGetValue(dllNameStr, out Assembly val))
                {
                    return val;
                }

                // Load DLLs where this program (exe) is located
                string dllPath = Path.Combine(GetExecutableDirectory(), "lib", dllNameStr);
                // Prefer to use Newtonsoft dll from game instead of our own due to protobuf issues. TODO: Remove when we do our own deserialization of game data instead of using the game's protobuf.
                if (dllPath.IndexOf("Newtonsoft.Json.dll", StringComparison.OrdinalIgnoreCase) >= 0 || !File.Exists(dllPath))
                {
                    // Try find game managed libraries
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        dllPath = Path.Combine(gameInstallDir.Value, "Resources", "Data", "Managed", dllNameStr);
                    }
                    else
                    {
                        dllPath = Path.Combine(gameInstallDir.Value, "Subnautica_Data", "Managed", dllNameStr);
                    }
                }

                try
                {
                    // Read assemblies as bytes as to not lock the file so that Nitrox can patch assemblies while server is running.
                    Assembly assembly = Assembly.Load(File.ReadAllBytes(dllPath));
                    return resolvedAssemblyCache[dllNameStr] = assembly;
                }
                catch
                {
                    return null;
                }
            }

            Assembly assembly = ResolveFromLib(args.Name);
            if (assembly == null && !args.Name.Contains(".resources"))
            {
                assembly = Assembly.Load(args.Name);
            }

            return assembly;
        }

        private static string GetExecutableDirectory()
        {
            if (currentExecutableDirectory != null)
            {
                return currentExecutableDirectory;
            }
            string pathAttempt = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrWhiteSpace(pathAttempt))
            {
                using Process proc = Process.GetCurrentProcess();
                pathAttempt = proc.MainModule?.FileName;
            }
            return currentExecutableDirectory = new Uri(Path.GetDirectoryName(pathAttempt ?? ".") ?? Directory.GetCurrentDirectory()).LocalPath;
        }
    }

    /// <summary>
    /// 启动 Web API Host 用于提供玩家信息查询接口
    /// </summary>
    private static async Task<IHost?> StartWebApiHostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var server = NitroxServiceLocator.LocateService<NitroxServer.Server>();
            int apiPort = server.Port + 1000; // 使用游戏服务器端口 + 1000 作为 API 端口
            
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseUrls($"http://localhost:{apiPort}")
                        .ConfigureServices(services =>
                        {
                            services.AddControllers();
                            services.AddCors(options =>
                            {
                                options.AddDefaultPolicy(policy =>
                                {
                                    policy.AllowAnyOrigin()
                                          .AllowAnyMethod()
                                          .AllowAnyHeader();
                                });
                            });
                        })
                        .Configure(app =>
                        {
                            app.UseRouting();
                            app.UseCors();
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapControllers();
                            });
                        });
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
                });
            
            var host = builder.Build();
            await host.StartAsync(cancellationToken);
            
            return host;
        }
        catch (Exception ex)
        {
            Log.Error($"启动 Web API 失败: {ex.Message}");
            Console.WriteLine($"[DEBUG] Web API 启动失败: {ex}");
            return null;
        }
    }
}
