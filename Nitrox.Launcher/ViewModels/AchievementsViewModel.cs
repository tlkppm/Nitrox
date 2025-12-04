using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.Models.Services;
using Nitrox.Launcher.ViewModels.Abstract;

namespace Nitrox.Launcher.ViewModels;

internal partial class AchievementsViewModel : RoutableViewModelBase
{
    private readonly AchievementService achievementService;
    [ObservableProperty]
    private AvaloniaList<Achievement> achievements = [];

    [ObservableProperty]
    private AvaloniaList<Achievement> beginnerAchievements = [];

    [ObservableProperty]
    private AvaloniaList<Achievement> serverAchievements = [];

    [ObservableProperty]
    private AvaloniaList<Achievement> multiplayerAchievements = [];

    [ObservableProperty]
    private AvaloniaList<Achievement> explorationAchievements = [];

    [ObservableProperty]
    private AvaloniaList<Achievement> technicalAchievements = [];

    [ObservableProperty]
    private int totalPoints;

    [ObservableProperty]
    private int unlockedCount;

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private double completionPercentage;

    public AchievementsViewModel(AchievementService achievementService)
    {
        this.achievementService = achievementService;
        InitializeAchievements();
    }

    private void InitializeAchievements()
    {
        var allAchievements = new List<Achievement>
        {
            // 新手成就
            new Achievement
            {
                Id = "first_launch",
                Title = "初次启动",
                Description = "首次启动 Nitrox 启动器",
                IconPath = "/Assets/Images/tabs-icons/play.png",
                Points = 10,
                IsUnlocked = true,
                UnlockedDate = DateTime.Now,
                Category = AchievementCategory.Beginner,
                Rarity = AchievementRarity.Common,
                Progress = 1,
                MaxProgress = 1
            },
            new Achievement
            {
                Id = "create_first_server",
                Title = "服务器管理员",
                Description = "创建你的第一个多人游戏服务器",
                IconPath = "/Assets/Images/tabs-icons/server.png",
                Points = 20,
                IsUnlocked = false,
                Category = AchievementCategory.Beginner,
                Rarity = AchievementRarity.Common,
                Progress = 0,
                MaxProgress = 1
            },
            new Achievement
            {
                Id = "join_first_game",
                Title = "深海探险家",
                Description = "加入你的第一场多人游戏",
                IconPath = "/Assets/Images/tabs-icons/community.png",
                Points = 15,
                IsUnlocked = false,
                Category = AchievementCategory.Beginner,
                Rarity = AchievementRarity.Common,
                Progress = 0,
                MaxProgress = 1
            },

            // 服务器成就
            new Achievement
            {
                Id = "server_uptime_24h",
                Title = "稳定运行",
                Description = "服务器连续运行 24 小时",
                IconPath = "/Assets/Images/tabs-icons/server.png",
                Points = 30,
                IsUnlocked = false,
                Category = AchievementCategory.Server,
                Rarity = AchievementRarity.Rare,
                Progress = 0,
                MaxProgress = 24
            },
            new Achievement
            {
                Id = "server_10_players",
                Title = "热门服务器",
                Description = "服务器同时在线玩家达到 10 人",
                IconPath = "/Assets/Images/tabs-icons/community.png",
                Points = 50,
                IsUnlocked = false,
                Category = AchievementCategory.Server,
                Rarity = AchievementRarity.Epic,
                Progress = 0,
                MaxProgress = 10
            },
            new Achievement
            {
                Id = "server_100_hours",
                Title = "资深管理员",
                Description = "服务器累计运行 100 小时",
                IconPath = "/Assets/Images/tabs-icons/server.png",
                Points = 100,
                IsUnlocked = false,
                Category = AchievementCategory.Server,
                Rarity = AchievementRarity.Legendary,
                Progress = 0,
                MaxProgress = 100
            },

            // 多人游戏成就
            new Achievement
            {
                Id = "play_with_friend",
                Title = "团队合作",
                Description = "与好友一起游玩 1 小时",
                IconPath = "/Assets/Images/tabs-icons/community.png",
                Points = 25,
                IsUnlocked = false,
                Category = AchievementCategory.Multiplayer,
                Rarity = AchievementRarity.Common,
                Progress = 0,
                MaxProgress = 60
            },
            new Achievement
            {
                Id = "multiplayer_10_games",
                Title = "联机老手",
                Description = "参与 10 场多人游戏",
                IconPath = "/Assets/Images/tabs-icons/play.png",
                Points = 40,
                IsUnlocked = false,
                Category = AchievementCategory.Multiplayer,
                Rarity = AchievementRarity.Rare,
                Progress = 0,
                MaxProgress = 10
            },

            // 探索成就
            new Achievement
            {
                Id = "explore_community",
                Title = "社区探索者",
                Description = "访问社区页面",
                IconPath = "/Assets/Images/tabs-icons/community.png",
                Points = 5,
                IsUnlocked = false,
                Category = AchievementCategory.Exploration,
                Rarity = AchievementRarity.Common,
                Progress = 0,
                MaxProgress = 1
            },
            new Achievement
            {
                Id = "read_blog",
                Title = "博客读者",
                Description = "阅读项目博客文章",
                IconPath = "/Assets/Images/tabs-icons/blog.png",
                Points = 10,
                IsUnlocked = false,
                Category = AchievementCategory.Exploration,
                Rarity = AchievementRarity.Common,
                Progress = 0,
                MaxProgress = 5
            },
            new Achievement
            {
                Id = "check_updates",
                Title = "保持更新",
                Description = "查看更新日志",
                IconPath = "/Assets/Images/tabs-icons/update.png",
                Points = 5,
                IsUnlocked = false,
                Category = AchievementCategory.Exploration,
                Rarity = AchievementRarity.Common,
                Progress = 0,
                MaxProgress = 1
            },

            // 技术成就
            new Achievement
            {
                Id = "customize_settings",
                Title = "个性化设置",
                Description = "自定义启动器设置",
                IconPath = "/Assets/Images/tabs-icons/options.png",
                Points = 15,
                IsUnlocked = false,
                Category = AchievementCategory.Technical,
                Rarity = AchievementRarity.Common,
                Progress = 0,
                MaxProgress = 1
            },
            new Achievement
            {
                Id = "backup_save",
                Title = "数据备份专家",
                Description = "创建服务器存档备份",
                IconPath = "/Assets/Images/tabs-icons/server.png",
                Points = 20,
                IsUnlocked = false,
                Category = AchievementCategory.Technical,
                Rarity = AchievementRarity.Rare,
                Progress = 0,
                MaxProgress = 1
            },
            new Achievement
            {
                Id = "sponsor_support",
                Title = "赞助支持者",
                Description = "查看赞助支持页面",
                IconPath = "/Assets/Images/tabs-icons/sponsor.png",
                Points = 10,
                IsUnlocked = false,
                Category = AchievementCategory.Technical,
                Rarity = AchievementRarity.Common,
                Progress = 0,
                MaxProgress = 1
            }
        };

        Achievements.AddRange(allAchievements);
        
        // 从存储加载成就进度
        achievementService.LoadAllAchievements(allAchievements);
        
        // 按类别分组
        BeginnerAchievements.AddRange(allAchievements.Where(a => a.Category == AchievementCategory.Beginner));
        ServerAchievements.AddRange(allAchievements.Where(a => a.Category == AchievementCategory.Server));
        MultiplayerAchievements.AddRange(allAchievements.Where(a => a.Category == AchievementCategory.Multiplayer));
        ExplorationAchievements.AddRange(allAchievements.Where(a => a.Category == AchievementCategory.Exploration));
        TechnicalAchievements.AddRange(allAchievements.Where(a => a.Category == AchievementCategory.Technical));

        UpdateStatistics();
    }
    
    public Achievement GetAchievement(string achievementId)
    {
        return Achievements.FirstOrDefault(a => a.Id == achievementId);
    }
    
    public void TriggerAchievement(string achievementId)
    {
        var achievement = GetAchievement(achievementId);
        if (achievement != null)
        {
            achievementService.UnlockAchievement(achievementId, achievement);
            UpdateStatistics();
        }
    }
    
    public void UpdateAchievementProgress(string achievementId, int progress)
    {
        var achievement = GetAchievement(achievementId);
        if (achievement != null)
        {
            achievementService.UpdateProgress(achievementId, achievement, progress);
            UpdateStatistics();
        }
    }

    private void UpdateStatistics()
    {
        TotalCount = Achievements.Count;
        UnlockedCount = Achievements.Count(a => a.IsUnlocked);
        TotalPoints = Achievements.Where(a => a.IsUnlocked).Sum(a => a.Points);
        CompletionPercentage = TotalCount > 0 ? (double)UnlockedCount / TotalCount * 100 : 0;
    }

    internal override async Task ViewContentLoadAsync(CancellationToken cancellationToken = default)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            UpdateStatistics();
        });
    }
}

