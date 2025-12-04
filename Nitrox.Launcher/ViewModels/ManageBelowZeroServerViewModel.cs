using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.Models.Services;
using Nitrox.Launcher.ViewModels.Abstract;
using NitroxModel.Helper;
using NitroxModel.Logger;

namespace Nitrox.Launcher.ViewModels;

internal partial class ManageBelowZeroServerViewModel : RoutableViewModelBase
{
    private readonly IKeyValueStore keyValueStore;
    private readonly DialogService dialogService;
    private readonly ServerService serverService;


    [ObservableProperty]
    private BelowZeroServerEntry? targetServer;

    [ObservableProperty]
    private string currentWeather = "Clear";

    [ObservableProperty]
    private float currentTemperature = -10.0f;

    [ObservableProperty]
    private string consoleInput = string.Empty;

    [ObservableProperty]
    private string consoleOutput = string.Empty;

    public ManageBelowZeroServerViewModel(IKeyValueStore keyValueStore, DialogService dialogService, ServerService serverService)
    {
        this.keyValueStore = keyValueStore;
        this.dialogService = dialogService;
        this.serverService = serverService;
    }

    public void SetTargetServer(BelowZeroServerEntry server)
    {
        TargetServer = server;
        if (server != null)
        {
            CurrentWeather = server.Weather;
            CurrentTemperature = server.Temperature;
        }
    }

    internal override Task ViewContentLoadAsync(CancellationToken cancellationToken)
    {
        RefreshServerStatusAsync();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task StartServerAsync()
    {
        if (TargetServer == null) return;

        try
        {
            await serverService.StartBelowZeroServerAsync(TargetServer);
            await RefreshServerStatusAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"启动服务器失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StopServerAsync()
    {
        if (TargetServer == null) return;

        try
        {
            await serverService.StopBelowZeroServerAsync(TargetServer);
            await RefreshServerStatusAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"停止服务器失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshServerStatusAsync()
    {
        // 刷新服务器状态
        if (TargetServer != null)
        {
            CurrentWeather = TargetServer.Weather;
            CurrentTemperature = TargetServer.Temperature;
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SendConsoleCommandAsync()
    {
        if (TargetServer == null || string.IsNullOrWhiteSpace(ConsoleInput)) return;

        try
        {
            await serverService.SendBelowZeroServerCommandAsync(TargetServer, ConsoleInput);
            ConsoleOutput += $"\n> {ConsoleInput}";
            ConsoleInput = string.Empty;
        }
        catch (Exception ex)
        {
            Log.Error($"发送命令失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void BackToServers()
    {
        ChangeViewToPrevious<BelowZeroServersViewModel>();
    }

    [RelayCommand]
    private async Task ChangeWeatherAsync(string weather)
    {
        if (TargetServer == null) return;

        TargetServer.Weather = weather;
        CurrentWeather = weather;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ChangeTemperatureAsync()
    {
        if (TargetServer == null) return;

        TargetServer.Temperature = CurrentTemperature;
        await Task.CompletedTask;
    }
}
