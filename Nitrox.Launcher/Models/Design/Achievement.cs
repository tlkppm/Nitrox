using System;

namespace Nitrox.Launcher.Models.Design;

public class Achievement
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string IconPath { get; set; }
    public int Points { get; set; }
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedDate { get; set; }
    public AchievementCategory Category { get; set; }
    public AchievementRarity Rarity { get; set; }
    public int Progress { get; set; }
    public int MaxProgress { get; set; }
    
    public double ProgressPercentage => MaxProgress > 0 ? (double)Progress / MaxProgress * 100 : 0;
    
    public string RarityText => Rarity switch
    {
        AchievementRarity.Common => "普通",
        AchievementRarity.Rare => "稀有",
        AchievementRarity.Epic => "史诗",
        AchievementRarity.Legendary => "传说",
        _ => "未知"
    };
}

public enum AchievementCategory
{
    Beginner,      // 新手
    Server,        // 服务器
    Multiplayer,   // 多人游戏
    Exploration,   // 探索
    Technical      // 技术
}

public enum AchievementRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

