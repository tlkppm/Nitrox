using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Nitrox.Launcher.Models.Design;

public partial class AnnouncementItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementType Type { get; set; } = AnnouncementType.Info;
    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Medium;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [ObservableProperty]
    private bool isRead;
    
    [ObservableProperty]
    private bool isActive = true;

    public string TypeIcon => Type switch
    {
        AnnouncementType.Info => "/Assets/Images/tabs-icons/info.png",
        AnnouncementType.Warning => "/Assets/Images/tabs-icons/warning.png",
        AnnouncementType.Feature => "/Assets/Images/tabs-icons/feature.png",
        AnnouncementType.Tips => "/Assets/Images/tabs-icons/tips.png",
        AnnouncementType.Bugfix => "/Assets/Images/tabs-icons/bugfix.png",
        _ => "/Assets/Images/tabs-icons/info.png"
    };

    public string FormattedDate => CreatedAt.ToString("MM/dd HH:mm");
}

public enum AnnouncementType
{
    Info,
    Warning,
    Feature,
    Improvement,
    Tips,
    Bugfix
}

public enum AnnouncementPriority
{
    Low,
    Normal,
    Medium,
    High,
    Critical
}
