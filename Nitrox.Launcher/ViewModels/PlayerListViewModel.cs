using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nitrox.Launcher.Models;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.ViewModels.Abstract;

namespace Nitrox.Launcher.ViewModels;

public partial class PlayerListViewModel : ModalViewModelBase
{
    private static readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    [ObservableProperty]
    private ServerEntry? serverEntry;

    [ObservableProperty]
    private ObservableCollection<PlayerInfo> players = [];

    [ObservableProperty]
    private int playerCount;

    [ObservableProperty]
    private int maxPlayers;

    [ObservableProperty]
    private string serverName = string.Empty;

    public void LoadServer(ServerEntry server)
    {
        ServerEntry = server;
        ServerName = server.Name ?? "未知服务器";
        MaxPlayers = server.MaxPlayers;
        PlayerCount = server.Players;
        
        // 注册玩家数量变化监听
        this.RegisterMessageListener<ServerStatusMessage, PlayerListViewModel>((message, model) =>
        {
            if (model.ServerEntry?.Process?.Id == message.ProcessId)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    model.PlayerCount = message.PlayerCount;
                    _ = model.RefreshPlayerListAsync();
                });
            }
        });
        
        _ = RefreshPlayerListAsync();
    }

    private async Task RefreshPlayerListAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => Players.Clear());

        // 检查是否是嵌入式服务器
        if (ServerEntry?.IsEmbedded == true && ServerEntry?.Process != null)
        {
            // 嵌入式模式：尝试获取真实玩家名称
            bool waitingForResponse = true;
            List<string> playerNames = new();
            
            void OutputHandler(object? sender, OutputLine line)
            {
                // 解析 "List of players (X/Y):" 和玩家名称
                string text = line.LogText?.Trim() ?? "";
                
                if (text.Contains("List of players"))
                {
                    // 开始接收玩家列表
                    playerNames.Clear();
                }
                else if (waitingForResponse && playerNames.Count == 0 && !string.IsNullOrWhiteSpace(text) && !text.StartsWith('['))
                {
                    // 解析玩家名称列表（逗号分隔）
                    string[] names = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string name in names)
                    {
                        string cleanName = name.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanName))
                        {
                            playerNames.Add(cleanName);
                        }
                    }
                    waitingForResponse = false;
                }
            }

            // 订阅输出事件
            ServerEntry.Process.Output.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (OutputLine line in e.NewItems)
                    {
                        OutputHandler(s, line);
                    }
                }
            };

            // 发送 list 命令
            await ServerEntry.Process.SendCommandAsync("list");

            // 等待响应（最多2秒）
            int waitCount = 0;
            while (waitingForResponse && waitCount < 20)
            {
                await Task.Delay(100);
                waitCount++;
            }

            // 更新UI
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Players.Clear();
                
                if (playerNames.Count > 0)
                {
                    // 显示真实玩家名称
                    foreach (string name in playerNames)
                    {
                        Players.Add(new PlayerInfo
                        {
                            Name = name,
                            ConnectionTime = "在线中"
                        });
                    }
                }
                else
                {
                    // 如果无法获取玩家名称，显示占位符
                    for (int i = 0; i < PlayerCount; i++)
                    {
                        Players.Add(new PlayerInfo
                        {
                            Name = $"玩家 {i + 1}",
                            ConnectionTime = "在线中"
                        });
                    }
                }
            });
        }
        else
        {
            // 外部模式：尝试通过 Web API 获取真实玩家信息
            bool apiSuccess = false;
            
            // 检查是否使用 Generic Host（UseGenericHost 为 true）
            if (ServerEntry?.UseGenericHost == true)
            {
                try
                {
                    // API 端口 = 游戏服务器端口 + 1000
                    int apiPort = ServerEntry.Port + 1000;
                    string apiUrl = $"http://localhost:{apiPort}/api/players";
                    
                    var response = await httpClient.GetFromJsonAsync<ApiPlayerListResponse>(apiUrl);
                    
                    if (response?.Success == true && response.Players != null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Players.Clear();
                            foreach (var player in response.Players)
                            {
                                Players.Add(new PlayerInfo
                                {
                                    Name = player.Name ?? "未知玩家",
                                    ConnectionTime = "在线中"
                                });
                            }
                        });
                        apiSuccess = true;
                    }
                }
                catch
                {
                    // API 调用失败，回退到占位符模式
                    apiSuccess = false;
                }
            }
            
            // 如果 API 调用失败或不使用 Generic Host，显示占位符
            if (!apiSuccess)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (PlayerCount > 0)
                    {
                        for (int i = 0; i < PlayerCount; i++)
                        {
                            Players.Add(new PlayerInfo
                            {
                                Name = $"玩家 {i + 1}",
                                ConnectionTime = "外部服务器模式"
                            });
                        }
                    }
                    else
                    {
                        Players.Add(new PlayerInfo
                        {
                            Name = "暂无玩家在线",
                            ConnectionTime = "外部服务器模式"
                        });
                    }
                });
            }
        }
    }

    [RelayCommand]
    public async Task Refresh()
    {
        await RefreshPlayerListAsync();
    }
}

public partial class PlayerInfo : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string connectionTime = string.Empty;
}

// API 响应数据模型
internal class ApiPlayerListResponse
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public List<ApiPlayerInfo>? Players { get; set; }
}

internal class ApiPlayerInfo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Permissions { get; set; }
    public string? GameMode { get; set; }
}

