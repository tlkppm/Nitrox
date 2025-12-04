using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NitroxModel;

/// <summary>
/// 支持的游戏类型枚举
/// </summary>
public enum GameType
{
    /// <summary>
    /// 深海迷航原版
    /// </summary>
    Subnautica,
    
    /// <summary>
    /// 深海迷航：零度之下
    /// </summary>
    SubnauticaBelowZero
}

public sealed record GameInfo
{
    public static readonly GameInfo Subnautica;

    public static readonly GameInfo SubnauticaBelowZero;

    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string DataFolder { get; init; }

    public required string ExeName { get; init; }

    public required int SteamAppId { get; init; }

    public required string MsStoreStartUrl { get; init; }
    
    /// <summary>
    /// 游戏类型枚举，用于启动器中的游戏选择
    /// </summary>
    public required GameType Type { get; init; }

    /// <summary>
    /// 获取所有支持的游戏信息
    /// </summary>
    public static GameInfo[] SupportedGames => [Subnautica, SubnauticaBelowZero];

    /// <summary>
    /// 根据游戏类型获取GameInfo
    /// </summary>
    public static GameInfo GetByType(GameType type) => type switch
    {
        GameType.Subnautica => Subnautica,
        GameType.SubnauticaBelowZero => SubnauticaBelowZero,
        _ => throw new ArgumentException($"不支持的游戏类型: {type}")
    };

    static GameInfo()
    {
        Subnautica = new GameInfo
        {
            Name = "Subnautica",
            FullName = "Subnautica",
            DataFolder = "Subnautica_Data",
            ExeName = "Subnautica.exe",
            SteamAppId = 264710,
            MsStoreStartUrl = @"ms-xbl-38616e6e:\\",
            Type = GameType.Subnautica
        };

        SubnauticaBelowZero = new GameInfo
        {
            Name = "SubnauticaZero",
            FullName = "Subnautica: Below Zero",
            DataFolder = "SubnauticaZero_Data",
            ExeName = "SubnauticaZero.exe",
            SteamAppId = 848450,
            MsStoreStartUrl = @"ms-xbl-6e27970f:\\",
            Type = GameType.SubnauticaBelowZero
        };

        // Fixup for OSX
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Subnautica = Subnautica with
            {
                ExeName = "Subnautica",
                DataFolder = Path.Combine("Resources", "Data")
            };
            
            SubnauticaBelowZero = SubnauticaBelowZero with
            {
                ExeName = "SubnauticaZero",
                DataFolder = Path.Combine("Resources", "Data")
            };
        }
    }

    private GameInfo()
    {
    }
}
