using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using NitroxModel;
using NitroxModel.Discovery;
using NitroxModel.Discovery.Models;
using static NitroxModel.Discovery.Models.GameLibraries;
using NitroxModel.Helper;
using NitroxModel.Logger;
using NitroxModel.Platforms.Store;

namespace Nitrox.Launcher.Models.Services;

/// <summary>
/// 游戏检测服务 - 套用Below Zero的检测逻辑进行增强
/// </summary>
public class GameDetectionService
{
    /// <summary>
    /// 已检测到的游戏安装信息
    /// </summary>
    public class DetectedGame
    {
        public required GameType Type { get; init; }
        public required GameInfo GameInfo { get; init; }
        public required string InstallPath { get; init; }
        public required Platform Platform { get; init; }
        public required bool IsAvailable { get; init; }
        public string DisplayName => $"{GameInfo.FullName} ({Platform})";
    }

    private static readonly Dictionary<GameType, DetectedGame?> gameCache = new();

    /// <summary>
    /// 检测所有可用的游戏安装
    /// 套用Below Zero的多平台检测逻辑
    /// </summary>
    public static async Task<List<DetectedGame>> DetectAllGamesAsync()
    {
        Log.Info("开始检测所有游戏安装...");
        var detectedGames = new List<DetectedGame>();

        foreach (var gameInfo in GameInfo.SupportedGames)
        {
            try
            {
                var gameInstalls = await DetectGameInstallsAsync(gameInfo);
                detectedGames.AddRange(gameInstalls);
            }
            catch (Exception ex)
            {
                Log.Error($"检测游戏 {gameInfo.FullName} 时出错: {ex.Message}");
            }
        }

        Log.Info($"检测完成，找到 {detectedGames.Count} 个游戏安装");
        return detectedGames;
    }

    /// <summary>
    /// 检测特定游戏的所有安装
    /// 参考Below Zero的Platform检测逻辑
    /// </summary>
    private static async Task<List<DetectedGame>> DetectGameInstallsAsync(GameInfo gameInfo)
    {
        var installs = new List<DetectedGame>();

        // Steam 检测
        var steamPath = await DetectSteamGameAsync(gameInfo);
        if (!string.IsNullOrEmpty(steamPath))
        {
            installs.Add(new DetectedGame
            {
                Type = gameInfo.Type,
                GameInfo = gameInfo,
                InstallPath = steamPath,
                Platform = Platform.STEAM,
                IsAvailable = File.Exists(Path.Combine(steamPath, gameInfo.ExeName))
            });
        }

        // Epic Games 检测
        var epicPath = DetectEpicGame(gameInfo);
        if (!string.IsNullOrEmpty(epicPath))
        {
            installs.Add(new DetectedGame
            {
                Type = gameInfo.Type,
                GameInfo = gameInfo,
                InstallPath = epicPath,
                Platform = Platform.EPIC,
                IsAvailable = File.Exists(Path.Combine(epicPath, gameInfo.ExeName))
            });
        }

        // Microsoft Store 检测
        var msStorePath = DetectMicrosoftStoreGame(gameInfo);
        if (!string.IsNullOrEmpty(msStorePath))
        {
            installs.Add(new DetectedGame
            {
                Type = gameInfo.Type,
                GameInfo = gameInfo,
                InstallPath = msStorePath,
                Platform = Platform.MICROSOFT,
                IsAvailable = File.Exists(Path.Combine(msStorePath, gameInfo.ExeName))
            });
        }

        // Discord Store 检测（参考Below Zero的检测方式）
        var discordPath = DetectDiscordGame(gameInfo);
        if (!string.IsNullOrEmpty(discordPath))
        {
            installs.Add(new DetectedGame
            {
                Type = gameInfo.Type,
                GameInfo = gameInfo,
                InstallPath = discordPath,
                Platform = Platform.DISCORD,
                IsAvailable = File.Exists(Path.Combine(discordPath, gameInfo.ExeName))
            });
        }

        return installs.Where(g => g.IsAvailable).ToList();
    }

    /// <summary>
    /// Steam游戏检测 - 套用Below Zero的Registry检测逻辑
    /// </summary>
    private static async Task<string?> DetectSteamGameAsync(GameInfo gameInfo)
    {
        try
        {
            // 使用Nitrox现有的游戏发现机制
            var results = await Task.Run(() => GameInstallationFinder.Instance.FindGame(gameInfo, STEAM));
            var steamResult = results.FirstOrDefault(r => r.IsOk && r.Origin == STEAM);
            if (steamResult != null && !string.IsNullOrEmpty(steamResult.Path))
            {
                // 验证这是否是我们要找的游戏类型
                var exePath = Path.Combine(steamResult.Path, gameInfo.ExeName);
                if (File.Exists(exePath))
                {
                    Log.Info($"Steam检测成功: {gameInfo.FullName} at {steamResult.Path}");
                    return steamResult.Path;
                }
            }

            // 如果标准检测失败，尝试直接检测Steam库
            Log.Info($"标准Steam检测失败，尝试直接检测Steam库中的{gameInfo.FullName}");
            return await DetectSteamGameDirectlyAsync(gameInfo);
        }
        catch (Exception ex)
        {
            Log.Warn($"Steam检测失败: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 直接检测Steam库中的游戏 - 增强的Below Zero检测逻辑
    /// </summary>
    private static async Task<string?> DetectSteamGameDirectlyAsync(GameInfo gameInfo)
    {
        try
        {
            // 常见的Steam库路径
            var commonSteamPaths = new List<string>();
            
            // Windows默认Steam路径
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                
                commonSteamPaths.AddRange(new[]
                {
                    Path.Combine(programFiles, "Steam", "steamapps", "common"),
                    Path.Combine(programFilesX86, "Steam", "steamapps", "common"),
                    @"C:\SteamLibrary\steamapps\common",
                    @"D:\SteamLibrary\steamapps\common",
                    @"E:\SteamLibrary\steamapps\common",
                    @"F:\SteamLibrary\steamapps\common"
                });
            }

            // 搜索游戏文件夹
            var searchNames = new[] { gameInfo.Name, "Subnautica", "SubnauticaZero" };
            
            return await Task.Run(() =>
            {
                foreach (var steamPath in commonSteamPaths.Where(Directory.Exists))
                {
                    foreach (var searchName in searchNames)
                    {
                        var gamePath = Path.Combine(steamPath, searchName);
                        if (Directory.Exists(gamePath))
                        {
                            var exePath = Path.Combine(gamePath, gameInfo.ExeName);
                            if (File.Exists(exePath))
                            {
                                Log.Info($"直接Steam检测成功: {gameInfo.FullName} at {gamePath}");
                                return gamePath;
                            }
                        }
                    }
                }
                return (string?)null;
            });
        }
        catch (Exception ex)
        {
            Log.Warn($"直接Steam检测失败: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Epic Games检测 - 基于Below Zero的注册表检测方法
    /// </summary>
    private static string? DetectEpicGame(GameInfo gameInfo)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher");
            if (key?.GetValue("AppDataPath") is string epicPath)
            {
                var manifestsPath = Path.Combine(epicPath, "Manifests");
                if (Directory.Exists(manifestsPath))
                {
                    foreach (var manifestFile in Directory.GetFiles(manifestsPath, "*.item"))
                    {
                        var content = File.ReadAllText(manifestFile);
                        if (content.Contains(gameInfo.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            // 简化的manifest解析
                            var lines = content.Split('\n');
                            foreach (var line in lines)
                            {
                                if (line.Contains("\"InstallLocation\"") && line.Contains(':'))
                                {
                                    var path = line.Split(':')[1].Trim(' ', '"', ',');
                                    if (Directory.Exists(path) && File.Exists(Path.Combine(path, gameInfo.ExeName)))
                                    {
                                        return path;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Epic Games检测失败: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Microsoft Store检测 - 参考Below Zero的UWP检测方法
    /// </summary>
    private static string? DetectMicrosoftStoreGame(GameInfo gameInfo)
    {
        try
        {
            // UWP应用通常安装在WindowsApps目录下
            var windowsAppsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
            if (Directory.Exists(windowsAppsPath))
            {
                var dirs = Directory.GetDirectories(windowsAppsPath, $"*{gameInfo.Name}*", SearchOption.TopDirectoryOnly);
                foreach (var dir in dirs)
                {
                    var exePath = Path.Combine(dir, gameInfo.ExeName);
                    if (File.Exists(exePath))
                    {
                        return dir;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Microsoft Store检测失败: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Discord Store检测 - 基于Below Zero的检测方法
    /// </summary>
    private static string? DetectDiscordGame(GameInfo gameInfo)
    {
        try
        {
            var discordGamesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord", "games");
            if (Directory.Exists(discordGamesPath))
            {
                var gameDirs = Directory.GetDirectories(discordGamesPath, $"*{gameInfo.Name}*", SearchOption.TopDirectoryOnly);
                foreach (var dir in gameDirs)
                {
                    var exePath = Path.Combine(dir, gameInfo.ExeName);
                    if (File.Exists(exePath))
                    {
                        return dir;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Discord检测失败: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 获取缓存的游戏检测结果
    /// </summary>
    public static DetectedGame? GetCachedGame(GameType gameType)
    {
        return gameCache.TryGetValue(gameType, out var game) ? game : null;
    }

    /// <summary>
    /// 设置缓存的游戏检测结果
    /// </summary>
    public static void SetCachedGame(GameType gameType, DetectedGame? game)
    {
        gameCache[gameType] = game;
    }
}
