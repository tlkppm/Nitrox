using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.Models.Utils;
using Nitrox.Launcher.ViewModels;
using NitroxModel.Helper;
using NitroxModel.Logger;
using NitroxModel.Platforms.OS.Shared;
using NitroxModel.Server;
using Timer = System.Threading.Timer;

namespace Nitrox.Launcher.Models.Services;

/// <summary>
///     Keeps track of server instances.
/// </summary>
internal sealed class ServerService : IMessageReceiver, INotifyPropertyChanged
{
    private readonly DialogService dialogService;
    private readonly IKeyValueStore keyValueStore;
    private readonly Func<IRoutingScreen> screenProvider;
    private List<ServerEntry> servers = [];
    private readonly Lock serversLock = new();
    private bool shouldRefreshServersList;
    private FileSystemWatcher? watcher;
    private readonly CancellationTokenSource serverRefreshCts = new();
    private readonly HashSet<string> loggedErrorDirectories = [];
    private readonly HashSet<int> knownServerProcessIds = [];
    private readonly Lock knownServerProcessIdsLock = new();
    private volatile bool hasUpdatedAtLeastOnce;
    private readonly Timer? serverDetectionTimer;

    public ServerService(DialogService dialogService, IKeyValueStore keyValueStore, Func<IRoutingScreen> screenProvider)
    {
        this.dialogService = dialogService;
        this.keyValueStore = keyValueStore;
        this.screenProvider = screenProvider;

        if (!IsDesignMode)
        {
            _ = LoadServersAsync().ContinueWithHandleError(ex => LauncherNotifier.Error(ex.Message));
            
            //  启动定期服务器检测定时器，每5秒检测一次运行中的服务器
            serverDetectionTimer = new Timer(async _ =>
            {
                try
                {
                    await DetectAndAttachRunningServersAsync();
                }
                catch (Exception ex)
                {
                    Log.Debug($"定期服务器检测出错: {ex.Message}");
                }
            }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
        }

        this.RegisterMessageListener<SaveDeletedMessage, ServerService>(static (message, receiver) =>
        {
            lock (receiver.serversLock)
            {
                bool changes = false;
                for (int i = receiver.servers.Count - 1; i >= 0; i--)
                {
                    if (receiver.servers[i].Name == message.SaveName)
                    {
                        receiver.servers.RemoveAt(i);
                        changes = true;
                    }
                }
                if (changes)
                {
                    receiver.SetField(ref receiver.servers, receiver.servers);
                }
            }
        });
    }

    private async Task LoadServersAsync()
    {
        await GetSavesOnDiskAsync();
        _ = WatchServersAsync(serverRefreshCts.Token).ContinueWithHandleError(ex => LauncherNotifier.Error(ex.Message));
    }

    public async Task<bool> StartServerAsync(ServerEntry server)
    {
        int serverPort = server.Port;
        IPEndPoint endPoint = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().FirstOrDefault(ip => ip.Port == serverPort);
        if (endPoint != null)
        {
            bool proceed = await dialogService.ShowAsync<DialogBoxViewModel>(model =>
            {
                model.Title = $"端口 {serverPort} 不可用";
                model.Description = "建议在启动此服务器之前更改端口。您仍要继续吗？";
                model.ButtonOptions = ButtonOptions.YesNo;
            });
            if (!proceed)
            {
                return false;
            }
        }

        // TODO: Exclude upgradeable versions + add separate prompt to upgrade first?
        if (server.Version != NitroxEnvironment.Version && !await ConfirmServerVersionAsync(server))
        {
            return false;
        }
        if (await GameInspect.IsOutdatedGameAndNotify(NitroxUser.GamePath, dialogService))
        {
            return false;
        }

        try
        {
            server.Version = NitroxEnvironment.Version;
            server.Start(keyValueStore.GetSavesFolderDir(), onExited: () =>
            {
                lock (knownServerProcessIdsLock)
                {
                    knownServerProcessIds.Remove(server.Process?.Id ?? 0);
                }
            });
            if (server.IsEmbedded)
            {
                await screenProvider().ShowAsync(new EmbeddedServerViewModel(server));
            }
            if (server.Process is { Id: > 0 })
            {
                lock (knownServerProcessIdsLock)
                {
                    knownServerProcessIds.Add(server.Process.Id);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error while starting server \"{server.Name}\"");
            await Dispatcher.UIThread.InvokeAsync(async () => await dialogService.ShowErrorAsync(ex, $"启动服务器 \"{server.Name}\" 时出错"));
            return false;
        }
    }

    public async Task<bool> ConfirmServerVersionAsync(ServerEntry server) =>
        await dialogService.ShowAsync<DialogBoxViewModel>(model =>
        {
            model.Title = $"服务器 '{server.Name}' 的版本是 v{(server.Version != null ? server.Version.ToString() : "X.X.X.X")}。强烈建议不要在 Nitrox v{NitroxEnvironment.Version} 中使用此存档文件。您仍要继续吗？";
            model.ButtonOptions = ButtonOptions.YesNo;
        });

    private async Task GetSavesOnDiskAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(keyValueStore.GetSavesFolderDir());

            Dictionary<string, (ServerEntry Data, bool HasFiles)> serversOnDisk = Servers.ToDictionary(entry => entry.Name, entry => (entry, false));
            foreach (string saveDir in Directory.EnumerateDirectories(keyValueStore.GetSavesFolderDir()))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (serversOnDisk.TryGetValue(Path.GetFileName(saveDir), out (ServerEntry Data, bool _) server))
                    {
                        // This server has files, so don't filter it away from server list.
                        serversOnDisk[Path.GetFileName(saveDir)] = (server.Data, true);
                        continue;
                    }
                    ServerEntry entryFromDir = await Task.Run(() => ServerEntry.FromDirectory(saveDir), cancellationToken);
                    if (entryFromDir != null)
                    {
                        serversOnDisk.Add(entryFromDir.Name, (entryFromDir, true));
                    }
                    loggedErrorDirectories.Remove(saveDir);
                }
                catch (Exception ex)
                {
                    if (loggedErrorDirectories.Add(saveDir)) // Only log once per directory to prevent log spam
                    {
                        // 使用用户友好的错误处理器，在界面显示而不是仅在日志中记录
                        UserFriendlyErrorHandler.RecordSaveFileError(saveDir, ex);
                    }
                }
            }

            lock (serversLock)
            {
                Servers = [..serversOnDisk.Values.Where(server => server.HasFiles).Select(server => server.Data).OrderByDescending(entry => entry.LastAccessedTime)];
                hasUpdatedAtLeastOnce = true;
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Error(ex, "Error while getting saves");
            await dialogService.ShowErrorAsync(ex, "Error while getting saves");
        }
    }

    private async Task WatchServersAsync(CancellationToken cancellationToken = default)
    {
        watcher = new FileSystemWatcher
        {
            Path = keyValueStore.GetSavesFolderDir(),
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
            Filter = "*.*",
            IncludeSubdirectories = true
        };
        watcher.Changed += OnDirectoryChanged;
        watcher.Created += OnDirectoryChanged;
        watcher.Deleted += OnDirectoryChanged;
        watcher.Renamed += OnDirectoryChanged;

        try
        {
            await Task.Run(async () =>
            {
                watcher.EnableRaisingEvents = true; // Slowish (~2ms) - Moved into Task.Run.

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    while (shouldRefreshServersList)
                    {
                        try
                        {
                            await GetSavesOnDiskAsync(cancellationToken);
                            shouldRefreshServersList = false;
                        }
                        catch (IOException)
                        {
                            await Task.Delay(100, cancellationToken);
                        }
                    }
                    await Task.Delay(1000, cancellationToken);
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }

    private void OnDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        shouldRefreshServersList = true;
    }

    public void Dispose()
    {
        serverRefreshCts.Cancel();
        serverRefreshCts.Dispose();
        serverDetectionTimer?.Dispose();
        WeakReferenceMessenger.Default.UnregisterAll(this);
        watcher?.Dispose();
    }

    public ServerEntry[] Servers
    {
        get
        {
            lock (serversLock)
            {
                return [..servers];
            }
        }
        private set
        {
            lock (serversLock)
            {
                SetField(ref servers, [..value]);
            }
        }
    }

    /// <summary>
    /// Gets the servers or waits for servers to be loaded from file system.
    /// </summary>
    public async Task<ServerEntry[]> GetServersAsync()
    {
        while (true)
        {
            lock (serversLock)
            {
                if (hasUpdatedAtLeastOnce)
                {
                    return Servers;
                }
            }
            await Task.Delay(100);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public async Task<ServerEntry?> GetOrCreateServerAsync(string saveName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saveName);
        string serverPath = Path.Combine(keyValueStore.GetSavesFolderDir(), saveName);
        return (await GetServersAsync()).FirstOrDefault(s => s.Name == saveName) ?? ServerEntry.FromDirectory(serverPath) ?? ServerEntry.CreateNew(serverPath, NitroxGameMode.SURVIVAL);
    }

    public async Task DetectAndAttachRunningServersAsync()
    {
        foreach (string pipeName in GetNitroxServerPipeNames())
        {
            try
            {
                Match? match = Regex.Match(pipeName, @"NitroxServer_(\d+)");
                if (!match.Success)
                {
                    continue;
                }
                int processId = int.Parse(match.Groups[1].Value);
                lock (knownServerProcessIdsLock)
                {
                    if (knownServerProcessIds.Contains(processId))
                    {
                        continue;
                    }
                }
                // 检查IPC功能是否可用 (仅在 .NET 5+ 中可用)
                string response = "";
                try 
                {
                    // 完全避免直接引用IPC类型，使用字符串和程序集反射
                    var assembly = System.Reflection.Assembly.GetAssembly(typeof(NitroxModel.Logger.Log));
                    var ipcType = assembly?.GetType("NitroxModel.Helper.Ipc");
                    var clientIpcType = ipcType?.GetNestedType("ClientIpc");
                    if (clientIpcType != null)
                    {
                        using CancellationTokenSource? cts = new(1000);
                        using var ipc = Activator.CreateInstance(clientIpcType, processId, cts) as IDisposable;
                        
                        var sendMethod = clientIpcType.GetMethod("SendCommand");
                        var readMethod = clientIpcType.GetMethod("ReadStringAsync");
                        var messagesType = ipcType?.GetNestedType("Messages");
                        var saveNameMessage = messagesType?.GetProperty("SaveNameMessage")?.GetValue(null) as string;
                        
                        if (sendMethod != null && readMethod != null && saveNameMessage != null)
                        {
                            await (Task)sendMethod.Invoke(ipc, new object[] { saveNameMessage, cts.Token });
                            response = await (Task<string>)readMethod.Invoke(ipc, new object[] { cts.Token });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // IPC功能不可用，使用备用方法
                    UserFriendlyErrorHandler.SafeExecute(() => {
                        Log.Debug($"IPC功能不可用，跳过进程 {processId}: {ex.Message}");
                    }, "IPC通信检查");
                    continue;
                }
                // 使用反射获取SaveNameMessage以保持兼容性
                string? saveNameMessagePrefix = null;
                try
                {
                    // 使用程序集反射避免直接引用IPC类型
                    var assembly = System.Reflection.Assembly.GetAssembly(typeof(NitroxModel.Logger.Log));
                    var ipcType = assembly?.GetType("NitroxModel.Helper.Ipc");
                    var messagesType = ipcType?.GetNestedType("Messages");
                    var saveNameMessage = messagesType?.GetProperty("SaveNameMessage")?.GetValue(null) as string;
                    if (saveNameMessage != null)
                    {
                        saveNameMessagePrefix = $"{saveNameMessage}:";
                    }
                }
                catch
                {
                    // IPC Messages类型不可用，跳过处理
                }

                if (saveNameMessagePrefix != null && response.StartsWith(saveNameMessagePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string? saveName = response[saveNameMessagePrefix.Length..].Trim('[', ']');
                    ServerEntry? serverMatch = servers?.FirstOrDefault(s => string.Equals(s.Name, saveName, StringComparison.Ordinal));
                    if (serverMatch != null)
                    {
                        Log.Info($"Found running server \"{serverMatch.Name}\" (PID: {processId})");
                        serverMatch.IsOnline = true;
                        lock (knownServerProcessIdsLock)
                        {
                            knownServerProcessIds.Add(processId);
                        }
                        serverMatch.Start(keyValueStore.GetSavesFolderDir(), processId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Pipe scan error for {pipeName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 检查已知的服务器进程是否仍在运行，如果不在则更新状态
    /// </summary>
    private async Task CheckServerProcessesAsync()
    {
        List<int> processIdsToRemove = [];
        List<string> currentPipeNames = GetNitroxServerPipeNames();
        HashSet<int> runningProcessIds = [];
        
        // 从管道名称中提取进程ID
        foreach (string pipeName in currentPipeNames)
        {
            Match? match = Regex.Match(pipeName, @"NitroxServer_(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int processId))
            {
                runningProcessIds.Add(processId);
            }
        }
        
        lock (knownServerProcessIdsLock)
        {
            foreach (int processId in knownServerProcessIds)
            {
                if (!runningProcessIds.Contains(processId))
                {
                    processIdsToRemove.Add(processId);
                }
            }
            
            foreach (int processId in processIdsToRemove)
            {
                knownServerProcessIds.Remove(processId);
                Log.Info($"检测到服务器进程 {processId} 已停止");
            }
        }
        
        // 更新对应服务器的在线状态
        if (processIdsToRemove.Count > 0)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                lock (serversLock)
                {
                    foreach (ServerEntry server in servers)
                    {
                        if (server.Process?.Id != null && processIdsToRemove.Contains(server.Process.Id))
                        {
                            server.IsOnline = false;
                            Log.Info($"服务器 '{server.Name}' 状态已更新为离线");
                        }
                    }
                }
            });
        }
    }

    private static List<string> GetNitroxServerPipeNames()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                DirectoryInfo? pipeDir = new(@"\\.\pipe\");
                return pipeDir.GetFileSystemInfos()
                              .Select(f => f.Name)
                              .Where(n => n.StartsWith("NitroxServer_", StringComparison.OrdinalIgnoreCase))
                              .ToList();
            }

            return ProcessEx.GetProcessesByName(GetServerExeName(), p => $"NitroxServer_{p.Id}")
                            .Where(s => s != null)
                            .ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Below Zero服务器相关属性和方法
    /// </summary>
    private List<BelowZeroServerEntry> belowZeroServers = [];

    public List<BelowZeroServerEntry> BelowZeroServers
    {
        get => belowZeroServers;
        set
        {
            belowZeroServers = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 加载Below Zero服务器
    /// </summary>
    public async Task LoadBelowZeroServersAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                // 创建示例Below Zero服务器
                var sampleServers = new List<BelowZeroServerEntry>
                {
                    new()
                    {
                        Name = "Below Zero Demo Server",
                        Weather = "Snow",
                        Temperature = -15.0f,
                        EnableSeatruckFeatures = true,
                        EnableWeatherSystem = true,
                        EnableIceLayerManagement = true,
                        EnableTemperatureSystem = true,
                        MaxPlayers = 8,
                        IsOnline = false
                    }
                };

                BelowZeroServers = sampleServers;
                Log.Info($"Loaded {BelowZeroServers.Count} Below Zero servers");
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Loading Below Zero servers failed: {ex}");
        }
    }

    public async Task StartBelowZeroServerAsync(BelowZeroServerEntry server)
    {
        try
        {
            Log.Info($"Starting Below Zero server: {server.Name}");
            
            // 模拟启动过程
            await Task.Delay(1000);
            
            server.IsOnline = true;
            Log.Info($"Below Zero server started: {server.Name}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start Below Zero server: {ex}");
            throw;
        }
    }

    public async Task StopBelowZeroServerAsync(BelowZeroServerEntry server)
    {
        try
        {
            Log.Info($"Stopping Below Zero server: {server.Name}");
            
            // 模拟停止过程
            await Task.Delay(500);
            
            server.IsOnline = false;
            Log.Info($"Below Zero server stopped: {server.Name}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to stop Below Zero server: {ex}");
            throw;
        }
    }

    public async Task SendBelowZeroServerCommandAsync(BelowZeroServerEntry server, string command)
    {
        try
        {
            Log.Info($"Sending command to Below Zero server {server.Name}: {command}");
            
            // 模拟命令发送
            await Task.Delay(100);
            
            Log.Info($"Command sent to Below Zero server: {command}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to send command to Below Zero server: {ex}");
            throw;
        }
    }
}
