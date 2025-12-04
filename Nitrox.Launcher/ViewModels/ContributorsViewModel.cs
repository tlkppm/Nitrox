using CommunityToolkit.Mvvm.Input;
using Nitrox.Launcher.ViewModels.Abstract;
using System.Collections.Generic;
using System;
using NitroxModel.Logger;

namespace Nitrox.Launcher.ViewModels;

internal partial class ContributorsViewModel : RoutableViewModelBase
{


    public override string ToString()
    {
        return "社区贡献者";
    }



    /// <summary>
    /// 核心开发团队
    /// </summary>
    public List<ContributorInfo> CoreTeam { get; } = new()
    {
        new ContributorInfo 
        { 
            Name = "SubnauticaNitrox Team", 
            Role = "核心开发团队",
            Icon = ""
        },
        new ContributorInfo 
        { 
            Name = "Sunrunner37", 
            Role = "项目创始人",
            Icon = ""
        },
        new ContributorInfo 
        { 
            Name = "Measurity", 
            Role = "首席开发者",
            Icon = ""
        },
        new ContributorInfo 
        { 
            Name = "tornac1234", 
            Role = "核心开发者",
            Icon = ""
        }
    };

    /// <summary>
    /// 中文社区制作团队
    /// </summary>
    public List<ContributorInfo> ChineseCommunity { get; }

    public ContributorsViewModel()
    {
        ChineseCommunity = new()
        {
            new ContributorInfo 
            { 
                Name = "夜樱神子", 
                Role = "社区制作",
                Contact = "QQ: 287834213",
                Icon = "",
                SocialLink = "https://b23.tv/3x37sTL",
                SocialIcon = "/Assets/Images/bibi.png"
            },
        new ContributorInfo 
        { 
            Name = "逃避现实の幻想乡", 
            Role = "宣传推广",
            Contact = "",
            Icon = "",
            SocialLink = "https://b23.tv/dhz3HX8",
            SocialIcon = "/Assets/Images/bibi.png"
        },
        new ContributorInfo 
        { 
            Name = "提纯源岩", 
            Role = "汉化工作",
            Contact = "QQ: 1214229031",
            Icon = "",
            SocialLink = "https://b23.tv/KMensBY",
            SocialIcon = "/Assets/Images/bibi.png"
        },
        new ContributorInfo 
        { 
            Name = "小潘", 
            Role = "游戏测试",
            Contact = "QQ: 2636558267",
            Icon = ""
        },
        new ContributorInfo 
        { 
            Name = "面包没睡醒", 
            Role = "问题修复与MOD宣传",
            Contact = "QQ: 3259267007",
            Icon = "",
            SocialLink = "https://b23.tv/GaGc4dC",
            SocialIcon = "/Assets/Images/bibi.png"
        },
        new ContributorInfo 
        { 
            Name = "无尽夏", 
            Role = "问题修复与MOD宣传",
            Contact = "QQ: 321728211",
            Icon = ""
        },
        new ContributorInfo 
        { 
            Name = "面包的游戏群", 
            Role = "社区支持与宣传",
            Contact = "QQ群: 2166053416",
            Icon = ""
        }
        };
        
        // 输出调试信息
        foreach (var contributor in ChineseCommunity)
        {
            Log.Info($"[贡献者] 名字: {contributor.Name}, SocialLink: '{contributor.SocialLink}', SocialIcon: '{contributor.SocialIcon}'");
        }
    }

    /// <summary>
    /// 国际社区贡献者
    /// </summary>
    public List<ContributorInfo> InternationalCommunity { get; } = new()
    {
        new ContributorInfo 
        { 
            Name = "GitHub Contributors", 
            Role = "代码贡献者",
            Icon = ""
        },
        new ContributorInfo 
        { 
            Name = "Community Translators", 
            Role = "本地化贡献者",
            Icon = ""
        },
        new ContributorInfo 
        { 
            Name = "Beta Testers", 
            Role = "测试贡献者",
            Icon = ""
        },
        new ContributorInfo 
        { 
            Name = "Bug Reporters", 
            Role = "问题反馈者",
            Icon = ""
        }
    };



    [RelayCommand]
    private void OpenGitHub()
    {
        OpenUri("github.com/SubnauticaNitrox/Nitrox/graphs/contributors");
    }

    [RelayCommand]
    private void OpenCommunityGroup()
    {
        // 复制QQ群号到剪贴板
        try
        {
            string groupNumber = "1019364297";
            // 这里可以添加复制到剪贴板的逻辑
            Log.Info($"深海迷航 Nitrox 开发交流群: {groupNumber}");
        }
        catch (Exception ex)
        {
            Log.Error($"无法打开社区群聊: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenUri(string? url)
    {
        Log.Info($"[OpenUri] 尝试打开链接: {url}");
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                GlobalStatic.OpenUri(url);
                Log.Info($"[OpenUri] 成功打开链接: {url}");
            }
            catch (Exception ex)
            {
                Log.Error($"无法打开链接 {url}: {ex.Message}");
            }
        }
        else
        {
            Log.Warn("[OpenUri] URL为空或null");
        }
    }


}

/// <summary>
/// 贡献者信息
/// </summary>
public class ContributorInfo
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Contact { get; set; } = "";
    public string Icon { get; set; } = "";
    public string SocialLink { get; set; } = "";
    public string SocialIcon { get; set; } = "";
}


