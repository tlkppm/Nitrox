using Nitrox.Launcher.Models.Attributes;
using Nitrox.Launcher.ViewModels;
using Nitrox.Launcher.Views.Abstract;

namespace Nitrox.Launcher.Views;

[ModalForViewModel(typeof(PlayerListViewModel))]
public partial class PlayerListModal : ModalBase
{
    public PlayerListModal()
    {
        InitializeComponent();
    }
}

