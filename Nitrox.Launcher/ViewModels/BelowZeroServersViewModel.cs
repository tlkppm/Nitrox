using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.Models.Services;
using Nitrox.Launcher.ViewModels.Abstract;
using NitroxModel.Helper;
using NitroxModel.Logger;

namespace Nitrox.Launcher.ViewModels;

internal partial class BelowZeroServersViewModel : RoutableViewModelBase
{
    private readonly IKeyValueStore keyValueStore;
    private readonly DialogService dialogService;
    private readonly ServerService serverService;
    private readonly ManageBelowZeroServerViewModel manageBelowZeroServerViewModel;
    
    [ObservableProperty]
    private AvaloniaList<BelowZeroServerEntry>? servers;

    public BelowZeroServersViewModel(IKeyValueStore keyValueStore, DialogService dialogService, ServerService serverService, ManageBelowZeroServerViewModel manageBelowZeroServerViewModel)
    {
        this.keyValueStore = keyValueStore;
        this.dialogService = dialogService;
        this.serverService = serverService;
        this.manageBelowZeroServerViewModel = manageBelowZeroServerViewModel;

        // 初始化服务器数据
        LoadInitialDataAsync();
    }

    private async void LoadInitialDataAsync()
    {
        try
        {
            await ViewContentLoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load initial data: {ex.Message}");
        }
    }

    internal override Task ViewContentLoadAsync(CancellationToken cancellationToken)
    {
        LoadServersAsync();
        return Task.CompletedTask;
    }

    private async Task LoadServersAsync()
    {
        try
        {
            Log.Info("正在加载Below Zero服务器列表...");
            
            // 从服务器服务加载Below Zero服务器
            await serverService.LoadBelowZeroServersAsync();
            
            // 更新UI绑定 - 转换为AvaloniaList
            Servers = new AvaloniaList<BelowZeroServerEntry>(serverService.BelowZeroServers);
            
            Log.Info($"已加载 {Servers?.Count ?? 0} 个Below Zero服务器");
        }
        catch (Exception ex)
        {
            Log.Error($"加载Below Zero服务器失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CreateServerAsync()
    {
        try
        {
            Log.Info("开始创建新的Below Zero服务器");
            
            // 创建Below Zero服务器
            var newServer = new BelowZeroServerEntry
            {
                Name = $"Below Zero Server {DateTime.Now:HH:mm:ss}",
                Weather = "Clear",
                Temperature = -10.0f,
                EnableSeatruckFeatures = true,
                EnableWeatherSystem = true,
                MaxPlayers = 8,
                IsOnline = false
            };
            
            serverService.BelowZeroServers.Add(newServer);
            await LoadServersAsync();
            
            Log.Info("Below Zero服务器创建成功");
        }
        catch (Exception ex)
        {
            Log.Error($"创建Below Zero服务器失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StartServerAsync(BelowZeroServerEntry server)
    {
        if (server == null)
        {
            return;
        }

        try
        {
            Log.Info($"启动Below Zero服务器: {server.Name}");
            
            // 启动服务器
            await serverService.StartBelowZeroServerAsync(server);
            
            Log.Info($"Below Zero服务器启动成功: {server.Name}");
        }
        catch (Exception ex)
        {
            Log.Error($"启动Below Zero服务器失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ManageServerAsync(BelowZeroServerEntry server)
    {
        if (server == null)
        {
            return;
        }

        try
        {
            Log.Info($"打开Below Zero服务器管理: {server.Name}");
            
            // 设置管理ViewModel的目标服务器
            manageBelowZeroServerViewModel.SetTargetServer(server);
            
            // 导航到管理页面
            ChangeView(manageBelowZeroServerViewModel);
        }
        catch (Exception ex)
        {
            Log.Error($"打开Below Zero服务器管理失败: {ex.Message}");
        }
    }
}
