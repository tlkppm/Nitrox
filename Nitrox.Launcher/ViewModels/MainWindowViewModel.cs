using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Nitrox.Launcher.Models;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.Models.Services;
using Nitrox.Launcher.Models.Utils;
using Nitrox.Launcher.ViewModels.Abstract;
using NitroxModel.Helper;
using NitroxModel.Logger;

namespace Nitrox.Launcher.ViewModels;

internal partial class MainWindowViewModel : ViewModelBase, IRoutingScreen
{
    private readonly BlogViewModel blogViewModel;
    private readonly CommunityViewModel communityViewModel;
    private readonly ContributorsViewModel contributorsViewModel;
    private readonly SponsorViewModel sponsorViewModel;
    private readonly DialogService dialogService;
    private readonly LaunchGameViewModel launchGameViewModel;
    private readonly Func<Window> mainWindowProvider;
    private readonly OptionsViewModel optionsViewModel;
    private readonly ServerService serverService;
    private readonly ServersViewModel serversViewModel;
    private readonly BelowZeroServersViewModel belowZeroServersViewModel;

    private readonly UpdatesViewModel updatesViewModel;
    private readonly AchievementsViewModel achievementsViewModel;
    
    public AchievementsViewModel AchievementsViewModel => achievementsViewModel;

    [ObservableProperty]
    private object? activeViewModel;

    [ObservableProperty]
    private bool updateAvailableOrUnofficial;

    public AvaloniaList<NotificationItem> Notifications { get; init; } = [];

    public MainWindowViewModel(
        Func<Window> mainWindowProvider,
        DialogService dialogService,
        ServersViewModel serversViewModel,
        BelowZeroServersViewModel belowZeroServersViewModel,
        LaunchGameViewModel launchGameViewModel,
        CommunityViewModel communityViewModel,
        BlogViewModel blogViewModel,
        ContributorsViewModel contributorsViewModel,
        SponsorViewModel sponsorViewModel,
        UpdatesViewModel updatesViewModel,
        AchievementsViewModel achievementsViewModel,
        OptionsViewModel optionsViewModel,
        ServerService serverService,
        IKeyValueStore keyValueStore
    )
    {
        this.mainWindowProvider = mainWindowProvider;
        this.dialogService = dialogService;
        this.launchGameViewModel = launchGameViewModel;
        this.serversViewModel = serversViewModel;
        this.belowZeroServersViewModel = belowZeroServersViewModel;
        this.communityViewModel = communityViewModel;
        this.blogViewModel = blogViewModel;
        this.contributorsViewModel = contributorsViewModel;
        this.sponsorViewModel = sponsorViewModel;
        this.updatesViewModel = updatesViewModel;
        this.achievementsViewModel = achievementsViewModel;
        this.optionsViewModel = optionsViewModel;
        this.serverService = serverService;

        this.RegisterMessageListener<ShowViewMessage, MainWindowViewModel>(static (message, vm) => vm.ShowAsync(message.ViewModel));
        this.RegisterMessageListener<ShowPreviousViewMessage, MainWindowViewModel>(static (message, vm) => vm.BackToAsync(message.RoutableViewModelType));
        this.RegisterMessageListener<NotificationAddMessage, MainWindowViewModel>(static async (message, vm) =>
        {
            vm.Notifications.Add(message.Item);
            await Task.Delay(7000);
            WeakReferenceMessenger.Default.Send(new NotificationCloseMessage(message.Item));
        });
        this.RegisterMessageListener<NotificationCloseMessage, MainWindowViewModel>(static async (message, vm) =>
        {
            message.Item.Dismissed = true;
            await Task.Delay(1000); // Wait for animations
            if (!IsDesignMode) // Prevent design preview crashes
            {
                vm.Notifications.Remove(message.Item);
            }
        });

        if (!IsDesignMode)
        {
            bool lightModeEnabled = keyValueStore.GetIsLightModeEnabled();
            Dispatcher.UIThread.Invoke(() => Application.Current!.RequestedThemeVariant = lightModeEnabled ? ThemeVariant.Light : ThemeVariant.Dark);

            if (!NitroxEnvironment.IsReleaseMode)
            {
                // Set debug default options here.
                keyValueStore.SetIsMultipleGameInstancesAllowed(true);
                LauncherNotifier.Info("You're now using Nitrox DEV build");
            }

            Task.Run(async () =>
            {
                if (!NetHelper.HasInternetConnectivity())
                {
                    Log.Warn("Launcher may not be connected to internet");
                    LauncherNotifier.Warning("Launcher may not be connected to internet");
                }
                UpdateAvailableOrUnofficial = await updatesViewModel.IsNitroxUpdateAvailableAsync();
            });

            // 触发首次启动成就
            achievementsViewModel.TriggerAchievement("first_launch");

            _ = this.ShowAsync(launchGameViewModel).ContinueWithHandleError(ex => LauncherNotifier.Error(ex.Message));
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task OpenLaunchGameViewAsync() => await this.ShowAsync(launchGameViewModel);

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task OpenServersViewAsync() => await this.ShowAsync(serversViewModel);
    
    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task OpenBelowZeroServersViewAsync() => await this.ShowAsync(belowZeroServersViewModel);

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task OpenCommunityViewAsync()
    {
        achievementsViewModel.TriggerAchievement("explore_community");
        await this.ShowAsync(communityViewModel);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task OpenBlogViewAsync()
    {
        achievementsViewModel.UpdateAchievementProgress("read_blog", 1);
        await this.ShowAsync(blogViewModel);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task OpenContributorsViewAsync() => await this.ShowAsync(contributorsViewModel);

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task OpenSponsorViewAsync()
    {
        achievementsViewModel.TriggerAchievement("sponsor_support");
        await this.ShowAsync(sponsorViewModel);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task OpenUpdatesViewAsync()
    {
        achievementsViewModel.TriggerAchievement("check_updates");
        await this.ShowAsync(updatesViewModel);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task OpenAchievementsViewAsync() => await this.ShowAsync(achievementsViewModel);

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task OpenOptionsViewAsync()
    {
        achievementsViewModel.TriggerAchievement("customize_settings");
        await this.ShowAsync(optionsViewModel);
    }

    [RelayCommand]
    public async Task ClosingAsync(WindowClosingEventArgs args)
    {
        ServerEntry[] embeddedServers = serverService.Servers.Where(s => s.IsOnline && s.IsEmbedded).ToArray();
        if (embeddedServers.Length > 0)
        {
            DialogBoxViewModel? result = await ShowDialogAsync(dialogService, args, $"{embeddedServers.Length} 个嵌入式服务器将停止，是否继续？");
            if (!result)
            {
                args.Cancel = true;
                return;
            }

            await HideWindowAndStopServersAsync(mainWindowProvider(), embeddedServers);
        }

        // As closing handler isn't async, cancellation might have happened anyway. So check manually if we should close the window after all the tasks are done.
        if (args.Cancel == false && mainWindowProvider().IsClosingByUser(args))
        {
            mainWindowProvider().CloseByCode();
        }

        static async Task<DialogBoxViewModel?> ShowDialogAsync(DialogService dialogService, WindowClosingEventArgs args, string title)
        {
            // Showing dialogs doesn't work if closing isn't set as 'cancelled'.
            bool prevCancelFlag = args.Cancel;
            args.Cancel = true;
            try
            {
                return await dialogService.ShowAsync<DialogBoxViewModel>(model =>
                {
                    model.Title = title;
                    model.ButtonOptions = ButtonOptions.YesNo;
                });
            }
            finally
            {
                args.Cancel = prevCancelFlag;
            }
        }

        static async Task HideWindowAndStopServersAsync(Window mainWindow, IEnumerable<ServerEntry> servers)
        {
            // Closing servers can take a while: hide the main window.
            mainWindow.Hide();
            try
            {
                await Task.WhenAll(servers.Select(s => s.StopAsync()));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}
