using System;
using System.Collections.Generic;
using System.Linq;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.Models.Utils;
using NitroxModel.Helper;
using NitroxModel.Logger;

namespace Nitrox.Launcher.Models.Services;

public class AchievementService
{
    private readonly IKeyValueStore keyValueStore;
    private const string ACHIEVEMENT_PREFIX = "achievement_";
    
    public AchievementService(IKeyValueStore keyValueStore)
    {
        this.keyValueStore = keyValueStore;
    }

    public void UnlockAchievement(string achievementId, Achievement achievement)
    {
        if (achievement.IsUnlocked)
        {
            return; // 已经解锁
        }

        achievement.IsUnlocked = true;
        achievement.UnlockedDate = DateTime.Now;
        
        // 保存到存储
        SaveAchievement(achievementId, achievement);
        
        // 显示通知
        LauncherNotifier.Success($"成就解锁: {achievement.Title} (+{achievement.Points}点)");
        
        Log.Info($"Achievement unlocked: {achievement.Title} ({achievementId})");
    }

    public void UpdateProgress(string achievementId, Achievement achievement, int progress)
    {
        if (achievement.IsUnlocked)
        {
            return;
        }

        achievement.Progress = Math.Min(progress, achievement.MaxProgress);
        
        // 保存进度
        SaveAchievement(achievementId, achievement);
        
        // 如果进度达到最大值，自动解锁
        if (achievement.Progress >= achievement.MaxProgress)
        {
            UnlockAchievement(achievementId, achievement);
        }
    }

    public void IncrementProgress(string achievementId, Achievement achievement, int increment = 1)
    {
        UpdateProgress(achievementId, achievement, achievement.Progress + increment);
    }

    public void SaveAchievement(string achievementId, Achievement achievement)
    {
        string key = ACHIEVEMENT_PREFIX + achievementId;
        string value = $"{achievement.IsUnlocked}|{achievement.Progress}|{achievement.UnlockedDate?.ToString("o") ?? ""}";
        keyValueStore.SetValue(key, value);
    }

    public void LoadAchievement(string achievementId, Achievement achievement)
    {
        string key = ACHIEVEMENT_PREFIX + achievementId;
        string value = keyValueStore.GetValue<string>(key, string.Empty);
        
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        string[] parts = value.Split('|');
        if (parts.Length >= 3)
        {
            if (bool.TryParse(parts[0], out bool isUnlocked))
            {
                achievement.IsUnlocked = isUnlocked;
            }
            
            if (int.TryParse(parts[1], out int progress))
            {
                achievement.Progress = progress;
            }
            
            if (!string.IsNullOrEmpty(parts[2]) && DateTime.TryParse(parts[2], out DateTime unlockedDate))
            {
                achievement.UnlockedDate = unlockedDate;
            }
        }
    }

    public void LoadAllAchievements(IEnumerable<Achievement> achievements)
    {
        foreach (var achievement in achievements)
        {
            LoadAchievement(achievement.Id, achievement);
        }
    }
}

