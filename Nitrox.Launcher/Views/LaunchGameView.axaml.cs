using Avalonia.Controls;
using Avalonia.Input;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.ViewModels;
using Nitrox.Launcher.Views.Abstract;

namespace Nitrox.Launcher.Views;

internal partial class LaunchGameView : RoutableViewBase<LaunchGameViewModel>
{
    public LaunchGameView()
    {
        InitializeComponent();
    }

    private async void AnnouncementCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && 
            border.DataContext is AnnouncementItem announcement && 
            DataContext is LaunchGameViewModel viewModel)
        {
            await viewModel.ShowAnnouncementDetailCommand.ExecuteAsync(announcement);
        }
    }
}
