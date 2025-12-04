using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NitroxModel.Helper;
using NitroxModel.Platforms.OS.Shared;
using NitroxModel.Platforms.OS.Windows;
using NitroxModel.Platforms.Store;

namespace NitroxModel.Helper;

/// <summary>
/// 增强的游戏验证系统，集成Steam协议验证
/// </summary>
public static class EnhancedPirateDetection
{
    private static readonly Steam steamPlatform = new();

    /// <summary>
    /// 验证结果枚举
    /// </summary>
    public enum ValidationResult
    {
        Valid,
        SteamNotRunning,
        UserNotLoggedIn,
        GameNotOwned,
        InvalidInstallation,
        PirateDetected
    }

    /// <summary>
    /// 执行完整的游戏所有权验证
    /// </summary>
    /// <param name="gameRootPath">游戏根目录路径</param>
    /// <returns>验证结果</returns>
    public static async Task<ValidationResult> ValidateGameOwnershipAsync(string gameRootPath)
    {
        try
        {
            Log.Info("开始执行增强游戏验证...");

            // 1. 首先执行原有的盗版检测
            if (PirateDetection.HasTriggered)
            {
                Log.Info("原有盗版检测已触发");
                return ValidationResult.PirateDetected;
            }

            // 2. 检查是否通过Steam安装
            if (!steamPlatform.OwnsGame(gameRootPath))
            {
                Log.Info("游戏不是通过Steam安装");
                return ValidationResult.GameNotOwned;
            }

            // 3. 检查Steam是否在运行并尝试启动
            ProcessEx? steamProcess = await steamPlatform.StartPlatformAsync();
            if (steamProcess == null)
            {
                Log.Info("Steam未运行且无法启动");
                return ValidationResult.SteamNotRunning;
            }

            // 4. 检查Steam用户登录状态
            if (!await IsSteamUserLoggedInAsync())
            {
                Log.Info("Steam用户未登录");
                return ValidationResult.UserNotLoggedIn;
            }

            Log.Info("Steam验证通过");
            return ValidationResult.Valid;
        }
        catch (Exception ex)
        {
            Log.Error($"Steam验证过程中出现错误: {ex.Message}");
            // 验证出错时，退回到宽松模式，仅依赖原有检测
            return PirateDetection.HasTriggered ? ValidationResult.PirateDetected : ValidationResult.Valid;
        }
    }

    /// <summary>
    /// 检查Steam用户是否已登录
    /// </summary>
    private static Task<bool> IsSteamUserLoggedInAsync()
    {
        try
        {
            // Windows平台检查Steam注册表
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                int activeUser = RegistryEx.Read<int>(@"SOFTWARE\Valve\Steam\ActiveProcess\ActiveUser", 0);
                return Task.FromResult(activeUser > 0);
            }

            // 其他平台简化处理 - 如果Steam进程能启动，假设用户已登录
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Debug($"检查Steam登录状态时出错: {ex.Message}");
            return Task.FromResult(true); // 出错时采用宽松策略
        }
    }

    /// <summary>
    /// 获取验证错误的本地化消息
    /// </summary>
    /// <param name="result">验证结果</param>
    /// <returns>本地化错误消息</returns>
    public static string GetValidationErrorMessage(ValidationResult result)
    {
        return result switch
        {
            ValidationResult.SteamNotRunning => "Steam客户端未运行，请先启动Steam",
            ValidationResult.UserNotLoggedIn => "请登录您的Steam账户",
            ValidationResult.GameNotOwned => "未找到游戏授权信息",
            ValidationResult.InvalidInstallation => "无法验证游戏文件完整性，请确保使用正版游戏并通过Steam验证文件",
            ValidationResult.PirateDetected => "游戏验证失败",
            _ => "游戏验证通过"
        };
    }

    /// <summary>
    /// 获取详细的解决方案说明
    /// </summary>
    /// <param name="result">验证结果</param>
    /// <returns>解决方案文本</returns>
    public static string GetValidationInstructions(ValidationResult result)
    {
        return result switch
        {
            ValidationResult.SteamNotRunning or 
            ValidationResult.UserNotLoggedIn or 
            ValidationResult.GameNotOwned => "解决方案：\n1. 启动Steam客户端\n2. 登录您的账户\n3. 验证游戏文件完整性",
            ValidationResult.InvalidInstallation or 
            ValidationResult.PirateDetected => "请确保使用正版游戏并通过Steam验证文件完整性",
            _ => "游戏验证通过，可以正常启动"
        };
    }
}
