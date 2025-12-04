using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.Models.Services;
using Nitrox.Launcher.Models.Utils;
using Nitrox.Launcher.ViewModels.Abstract;
using NitroxModel;
using NitroxModel.Discovery.Models;
using NitroxModel.Helper;
using static NitroxModel.Helper.EnhancedPirateDetection;
using NitroxModel.Logger;
using NitroxModel.Platforms.OS.Shared;
using NitroxModel.Platforms.Store;
using NitroxModel.Platforms.Store.Interfaces;

namespace Nitrox.Launcher.ViewModels;

internal partial class LaunchGameViewModel(DialogService dialogService, ServerService serverService, OptionsViewModel optionsViewModel, IKeyValueStore keyValueStore, AnnouncementService announcementService)
    : RoutableViewModelBase
{
    public static Task<string>? LastFindSubnauticaTask;
    private static bool hasInstantLaunched;
    private readonly DialogService dialogService = dialogService;
    private readonly IKeyValueStore keyValueStore = keyValueStore;

    private readonly ServerService serverService = serverService;
    private readonly AnnouncementService announcementService = announcementService;

    [RelayCommand]
    private async Task ShowAnnouncementDetail(AnnouncementItem announcement)
    {
        await dialogService.ShowAsync<AnnouncementDetailViewModel>(model =>
        {
            model.Announcement = announcement;
        });
    }

    [ObservableProperty]
    private Platform gamePlatform;

    [ObservableProperty]
    private string? platformToolTip;

    /// <summary>
    /// 当前选择的游戏类型
    /// </summary>
    [ObservableProperty]
    private GameType selectedGameType = GameType.Subnautica;

    /// <summary>
    /// 检测到的游戏安装列表
    /// </summary>
    [ObservableProperty]
    private List<GameDetectionService.DetectedGame> detectedGames = [];

    /// <summary>
    /// 当前选择的游戏安装
    /// </summary>
    [ObservableProperty]
    private GameDetectionService.DetectedGame? selectedGameInstall;

    /// <summary>
    /// 激活的公告列表
    /// </summary>
    [ObservableProperty]
    private List<AnnouncementItem> activeAnnouncements = [];

    /// <summary>
    /// 是否有未读公告
    /// </summary>
    [ObservableProperty]
    private bool hasUnreadAnnouncements;



    /// <summary>
    /// 是否正在检测游戏
    /// </summary>
    [ObservableProperty]
    private bool isDetectingGames;

    public Bitmap[] GalleryImageSources { get; } =
    [
        AssetHelper.GetAssetFromStream("/Assets/Images/gallery/image-1.png", static stream => new Bitmap(stream)),
        AssetHelper.GetAssetFromStream("/Assets/Images/gallery/image-2.png", static stream => new Bitmap(stream)),
        AssetHelper.GetAssetFromStream("/Assets/Images/gallery/image-3.png", static stream => new Bitmap(stream)),
        AssetHelper.GetAssetFromStream("/Assets/Images/gallery/image-4.png", static stream => new Bitmap(stream))
    ];

    public string Version => $"{NitroxEnvironment.ReleasePhase} {NitroxEnvironment.Version}";
    public string SubnauticaLaunchArguments => keyValueStore.GetSubnauticaLaunchArguments();

    /// <summary>
    /// 支持的游戏类型列表
    /// </summary>
    public List<GameType> SupportedGameTypes { get; } = [GameType.Subnautica, GameType.SubnauticaBelowZero];

    /// <summary>
    /// 游戏类型显示名称
    /// </summary>
    public string GetGameTypeDisplayName(GameType gameType) => gameType switch
    {
        GameType.Subnautica => "深海迷航",
        GameType.SubnauticaBelowZero => "深海迷航：零度之下",
        _ => gameType.ToString()
    };

    internal override async Task ViewContentLoadAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            NitroxUser.GamePlatformChanged += UpdateGamePlatform;
            UpdateGamePlatform();
            HandleInstantLaunchForDevelopment();
        }, cancellationToken);

        // 自动检测游戏安装
        await DetectGamesAsync();
        
        // 初始化公告系统
        InitializeAnnouncements();
    }

    internal override Task ViewContentUnloadAsync()
    {
        NitroxUser.GamePlatformChanged -= UpdateGamePlatform;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 检测游戏安装命令
    /// </summary>
    [RelayCommand]
    public async Task DetectGamesAsync()
    {
        if (IsDetectingGames)
        {
            return;
        }

        IsDetectingGames = true;
        try
        {
            Log.Info("开始检测游戏安装...");
            var games = await GameDetectionService.DetectAllGamesAsync();
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DetectedGames = games;
                
                // 自动选择第一个可用的游戏
                if (DetectedGames.Count > 0)
                {
                    var preferredGame = DetectedGames.FirstOrDefault(g => g.Type == SelectedGameType) 
                                       ?? DetectedGames.FirstOrDefault();
                    
                    if (preferredGame != null)
                    {
                        SelectedGameInstall = preferredGame;
                        SelectedGameType = preferredGame.Type;
                        GamePlatform = preferredGame.Platform;
                        Log.Info($"自动选择游戏: {preferredGame.DisplayName}");
                    }
                }
                
                Log.Info($"检测完成，找到 {DetectedGames.Count} 个游戏安装");
            });
        }
        catch (Exception ex)
        {
            Log.Error($"检测游戏时出错: {ex.Message}");
            await dialogService.ShowErrorAsync(ex, "检测游戏失败");
        }
        finally
        {
            IsDetectingGames = false;
        }
    }

    /// <summary>
    /// 切换游戏类型
    /// </summary>
    [RelayCommand]
    public async Task SwitchGameTypeAsync(GameType gameType)
    {
        if (SelectedGameType == gameType)
        {
            return;
        }

        SelectedGameType = gameType;
        
        // 查找对应的游戏安装
        var gameInstall = DetectedGames.FirstOrDefault(g => g.Type == gameType);
        if (gameInstall != null)
        {
            SelectedGameInstall = gameInstall;
            GamePlatform = gameInstall.Platform;
        }
        else
        {
            // 如果没有找到，重新检测
            await DetectGamesAsync();
        }
    }

    [RelayCommand]
    private async Task StartSingleplayerAsync()
    {
        if (GameInspect.WarnIfGameProcessExists(GameInfo.Subnautica) && !keyValueStore.GetIsMultipleGameInstancesAllowed())
        {
            return;
        }

        Log.Info("Launching Subnautica in singleplayer mode");
        try
        {
            if (string.IsNullOrWhiteSpace(NitroxUser.GamePath) || !Directory.Exists(NitroxUser.GamePath))
            {
                ChangeView(optionsViewModel);
                LauncherNotifier.Warning("Location of Subnautica is unknown. Set the path to it in settings");
                return;
            }

            NitroxEntryPatch.Remove(NitroxUser.GamePath);
            await StartGameAsync(null, GameInfo.GetByType(SelectedGameType), SelectedGameInstall?.InstallPath ?? NitroxUser.GamePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while starting game in singleplayer mode:");
            await dialogService.ShowErrorAsync(ex, "Error while starting game in singleplayer mode");
        }
    }

    [RelayCommand]
    private async Task StartMultiplayerAsync(string[]? args = null)
    {
        // 根据选择的游戏类型获取游戏信息
        var currentGameInfo = GameInfo.GetByType(SelectedGameType);
        var gameDisplayName = GetGameTypeDisplayName(SelectedGameType);
        
        Log.Info($"启动 {gameDisplayName} 多人游戏模式");
        try
        {
            bool setupResult = await Task.Run(async () =>
            {
                // 使用选择的游戏安装路径，如果有的话
                string gamePath = SelectedGameInstall?.InstallPath ?? NitroxUser.GamePath;
                
                if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
                {
                    ChangeView(optionsViewModel);
                    LauncherNotifier.Warning($"{gameDisplayName} 的安装位置未知。请在设置中设置路径");
                    return false;
                }
        // 使用增强的Steam协议验证
        var validationResult = await EnhancedPirateDetection.ValidateGameOwnershipAsync(gamePath);
        if (validationResult != ValidationResult.Valid)
        {
            string errorMessage = EnhancedPirateDetection.GetValidationErrorMessage(validationResult);
            string instructions = EnhancedPirateDetection.GetValidationInstructions(validationResult);
            
            LauncherNotifier.Error($"{errorMessage}\n\n{instructions}");
            return false;
        }
                if (GameInspect.WarnIfGameProcessExists(currentGameInfo) && !keyValueStore.GetIsMultipleGameInstancesAllowed())
                {
                    return false;
                }
                if (await GameInspect.IsOutdatedGameAndNotify(gamePath, dialogService))
                {
                    return false;
                }

                // TODO: The launcher should override FileRead win32 API for the Subnautica process to give it the modified Assembly-CSharp from memory
                try
                {
                    const string PATCHER_DLL_NAME = "NitroxPatcher.dll";

                    string patcherDllPath = Path.Combine(NitroxUser.ExecutableRootPath ?? "", "lib", "net472", PATCHER_DLL_NAME);
                    if (!File.Exists(patcherDllPath))
                    {
                        LauncherNotifier.Error("Launcher files seems corrupted, please contact us");
                        return false;
                    }

                    File.Copy(
                        patcherDllPath,
                        Path.Combine(gamePath, currentGameInfo.DataFolder, "Managed", PATCHER_DLL_NAME),
                        true
                    );
                }
                catch (IOException ex)
                {
                    Log.Error(ex, "Unable to move initialization dll to Managed folder. Still attempting to launch because it might exist from previous runs");
                }

                // Try inject Nitrox into game code.
                if (LastFindSubnauticaTask != null)
                {
                    await LastFindSubnauticaTask;
                }
                await NitroxEntryPatch.Apply(gamePath);

                if (QModHelper.IsQModInstalled(gamePath))
                {
                    Log.Warn("Seems like QModManager is installed");
                    LauncherNotifier.Warning("QModManager Detected in the game folder");
                }

                return true;
            });

            if (!setupResult)
            {
                return;
            }

            await StartGameAsync(args, currentGameInfo, SelectedGameInstall?.InstallPath ?? NitroxUser.GamePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while starting game in multiplayer mode:");
            await dialogService.ShowErrorAsync(ex, "Error while starting game in multiplayer mode");
        }
    }

    [RelayCommand]
    private void OpenContributionsOfYear()
    {
        string fromValue = HttpUtility.UrlEncode($"{DateTime.UtcNow.AddYears(-1):M/d/yyyy}");
        string toValue = HttpUtility.UrlEncode($"{DateTime.UtcNow:M/d/yyyy}");
        OpenUri($"github.com/SubnauticaNitrox/Nitrox/graphs/contributors?from={fromValue}&to={toValue}");
    }

    /// <summary>
    ///     Launches the server and Subnautica immediately if instant launch is active.
    /// </summary>
    [Conditional("DEBUG")]
    private void HandleInstantLaunchForDevelopment()
    {
        if (hasInstantLaunched)
        {
            return;
        }
        hasInstantLaunched = true;
        if (App.InstantLaunch == null)
        {
            return;
        }
        Task.Run(async () =>
        {
            // Start the server
            ServerEntry? server = await serverService.GetOrCreateServerAsync(App.InstantLaunch.SaveName);
            if (server == null)
            {
                throw new Exception("Failed to create new server save files");
            }
            server.Name = App.InstantLaunch.SaveName;
            Task serverStartTask = Dispatcher.UIThread.InvokeAsync(async () => await serverService.StartServerAsync(server)).ContinueWithHandleError();
            // Start a game in multiplayer for each player
            foreach (string playerName in App.InstantLaunch.PlayerNames)
            {
                await StartMultiplayerAsync(["--instantlaunch", playerName]).ContinueWithHandleError();
            }

            await serverStartTask;
        }).ContinueWithHandleError();
    }

    /// <summary>
    /// 通用游戏启动方法 - 支持多游戏版本
    /// 套用Below Zero的启动逻辑进行增强
    /// </summary>
    private async Task StartGameAsync(string[]? args, GameInfo gameInfo, string gamePath)
    {
        var gameDisplayName = GetGameTypeDisplayName(gameInfo.Type);
        LauncherNotifier.Info($"正在启动 {gameDisplayName}");
        
        // 应用游戏特定的注入入口点
        await ApplyGameSpecificPatches(gameInfo, gamePath);
        
        string gameLaunchArguments = $"{SubnauticaLaunchArguments} {string.Join(" ", args ?? Environment.GetCommandLineArgs())}";
        string gameExe;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            gameExe = Path.Combine(gamePath, "MacOS", gameInfo.ExeName);
        }
        else
        {
            gameExe = Path.Combine(gamePath, gameInfo.ExeName);
        }

        if (!File.Exists(gameExe))
        {
            throw new FileNotFoundException($"无法找到 {gameDisplayName} 可执行文件: {gameExe}");
        }

        IGamePlatform platform = GamePlatforms.GetPlatformByGameDir(gamePath);

        // 开始游戏和游戏平台
        using ProcessEx game = platform switch
        {
            Steam s => await s.StartGameAsync(gameExe, gameLaunchArguments, gameInfo.SteamAppId, ProcessEx.ProcessExists(gameInfo.ExeName) && keyValueStore.GetIsMultipleGameInstancesAllowed()),
            EpicGames e => await e.StartGameAsync(gameExe, gameLaunchArguments),
            MSStore m => await m.StartGameAsync(gameExe, gameLaunchArguments),
            Discord d => await d.StartGameAsync(gameExe, gameLaunchArguments),
            _ => throw new Exception($"目录 '{gamePath}' 不是有效的 {gameInfo.Name} 游戏安装，或者游戏平台不受 Nitrox 支持。")
        };

        if (game is null)
        {
            throw new Exception($"游戏无法通过 {platform.Name} 启动");
        }
    }

    /// <summary>
    /// 应用游戏特定的补丁和注入入口点
    /// </summary>
    private async Task ApplyGameSpecificPatches(GameInfo gameInfo, string gamePath)
    {
        try
        {
            if (gameInfo.Type == GameType.SubnauticaBelowZero)
            {
                Log.Info("应用Below Zero游戏注入入口点");
                
                // 检查是否已经应用了Below Zero补丁
                if (!BelowZeroEntryPatch.IsPatchApplied(gamePath))
                {
                    LauncherNotifier.Info("正在应用Below Zero Nitrox入口点...");
                    await BelowZeroEntryPatch.Apply(gamePath);
                    LauncherNotifier.Success("Below Zero Nitrox入口点应用成功");
                }
                else
                {
                    Log.Debug("Below Zero补丁已存在，跳过应用");
                }
            }
            else if (gameInfo.Type == GameType.Subnautica)
            {
                Log.Info("应用原版深海迷航游戏注入入口点");
                
                // 检查是否已经应用了原版补丁
                if (!NitroxEntryPatch.IsPatchApplied(gamePath))
                {
                    LauncherNotifier.Info("正在应用Nitrox入口点...");
                    await NitroxEntryPatch.Apply(gamePath);
                    LauncherNotifier.Success("Nitrox入口点应用成功");
                }
                else
                {
                    Log.Debug("原版Nitrox补丁已存在，跳过应用");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"应用游戏补丁失败: {ex.Message}");
            LauncherNotifier.Error($"应用游戏补丁失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 兼容性方法 - 保持向后兼容
    /// </summary>
    private async Task StartSubnauticaAsync(string[]? args = null)
    {
        await StartGameAsync(args, GameInfo.Subnautica, NitroxUser.GamePath);
    }

    private void UpdateGamePlatform()
    {
        GamePlatform = NitroxUser.GamePlatform?.Platform ?? Platform.NONE;
        PlatformToolTip = GamePlatform.GetAttribute<DescriptionAttribute>().Description;
    }

    /// <summary>
    /// 刷新公告列表
    /// </summary>
    private void RefreshAnnouncements()
    {
        try
        {
            var allAnnouncements = announcementService.GetAnnouncements();
            ActiveAnnouncements = allAnnouncements.Where(a => a.IsActive).OrderByDescending(a => a.CreatedAt).ToList();
            HasUnreadAnnouncements = false;
        }
        catch (Exception ex)
        {
            Log.Error($"刷新公告失败: {ex.Message}");
        }
    }


    /// <summary>
    /// 初始化时加载公告
    /// </summary>
    public void InitializeAnnouncements()
    {
        try
        {
            if (announcementService != null)
            {
                RefreshAnnouncements();
                Log.Info($"公告系统已初始化，共有 {ActiveAnnouncements?.Count ?? 0} 条活跃公告");
            }
            else
            {
                Log.Error("AnnouncementService 为空，无法初始化公告");
                // 设置默认空列表
                ActiveAnnouncements = [];
                HasUnreadAnnouncements = false;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"初始化公告系统失败: {ex.Message}");
            // 设置默认空列表以防止UI错误
            ActiveAnnouncements = [];
            HasUnreadAnnouncements = false;
        }
    }
}
