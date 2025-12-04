using Nitrox.Launcher.Models.Attributes;
using Nitrox.Launcher.ViewModels;
using Nitrox.Launcher.Views.Abstract;

namespace Nitrox.Launcher.Views;

[ModalForViewModel(typeof(AnnouncementDetailViewModel))]
public partial class AnnouncementDetailModal : ModalBase
{
    public AnnouncementDetailModal()
    {
        InitializeComponent();
    }
}

