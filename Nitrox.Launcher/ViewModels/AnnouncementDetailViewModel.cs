using CommunityToolkit.Mvvm.ComponentModel;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.ViewModels.Abstract;

namespace Nitrox.Launcher.ViewModels;

/// <summary>
/// 公告详情弹窗ViewModel
/// </summary>
public partial class AnnouncementDetailViewModel : ModalViewModelBase
{
    [ObservableProperty]
    private AnnouncementItem? announcement;

    public string PriorityText => Announcement?.Priority switch
    {
        AnnouncementPriority.Critical => "严重",
        AnnouncementPriority.High => "高",
        AnnouncementPriority.Medium => "中",
        AnnouncementPriority.Normal => "普通",
        AnnouncementPriority.Low => "低",
        _ => "普通"
    };

    public string TypeText => Announcement?.Type switch
    {
        AnnouncementType.Info => "信息",
        AnnouncementType.Warning => "警告",
        AnnouncementType.Feature => "新功能",
        AnnouncementType.Tips => "提示",
        AnnouncementType.Bugfix => "Bug修复",
        _ => "信息"
    };
}

