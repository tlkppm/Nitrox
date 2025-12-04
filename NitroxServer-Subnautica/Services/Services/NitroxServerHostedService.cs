#if NET6_0_OR_GREATER && GENERIC_HOST_AVAILABLE
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NitroxModel.Core;
using NitroxServer;

namespace NitroxServer_Subnautica.Services;

/// <summary>
/// 将现有的Nitrox Server包装为Generic Host托管服务
/// 这样可以在不破坏现有功能的情况下利用Generic Host的优势
/// </summary>
public class NitroxServerHostedService : BackgroundService
{
    private readonly ILogger<NitroxServerHostedService> logger;
    private readonly IHostApplicationLifetime hostLifetime;
    
    private Server nitroxServer;
    private string serverSaveName;

    public NitroxServerHostedService(
        ILogger<NitroxServerHostedService> logger,
        IHostApplicationLifetime hostLifetime)
    {
        this.logger = logger;
        this.hostLifetime = hostLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("正在启动Nitrox服务器 (Generic Host模式)...");

            // 从现有的DI容器获取Server实例
            nitroxServer = NitroxServiceLocator.LocateService<Server>();
            
            // 从命令行参数获取保存名称（与原逻辑保持一致）
            string[] args = Environment.GetCommandLineArgs();
            serverSaveName = Server.GetSaveName(args, "My World");
            
            logger.LogInformation("正在等待端口可用: {Port}", nitroxServer.Port);
            await WaitForPortAvailableAsync(nitroxServer.Port, stoppingToken);

            logger.LogInformation("正在启动Nitrox服务器...");
            bool serverStarted = nitroxServer.Start(serverSaveName, CancellationTokenSource.CreateLinkedTokenSource(stoppingToken));
            
            if (!serverStarted)
            {
                logger.LogError("服务器启动失败");
                hostLifetime.StopApplication();
                return;
            }

            logger.LogInformation("Nitrox服务器启动成功");
            
            // 注册玩家数量变化事件
            nitroxServer.PlayerCountChanged += OnPlayerCountChanged;

            // 等待取消信号
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "服务器运行时发生错误");
            hostLifetime.StopApplication();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("正在停止Nitrox服务器...");

        try
        {
            // 取消注册事件
            if (nitroxServer != null)
            {
                nitroxServer.PlayerCountChanged -= OnPlayerCountChanged;
                
                // 调用现有的停止逻辑
                nitroxServer.Stop(true);
            }

            logger.LogInformation("Nitrox服务器已停止");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "停止服务器时发生错误");
        }

        await base.StopAsync(cancellationToken);
    }

    private void OnPlayerCountChanged(int playerCount)
    {
        logger.LogDebug("玩家数量变化: {PlayerCount}", playerCount);
    }

    /// <summary>
    /// 等待端口可用
    /// </summary>
    private async Task WaitForPortAvailableAsync(int port, CancellationToken cancellationToken)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(30);
        DateTime startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                IPEndPoint[] listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
                bool portInUse = false;
                
                foreach (IPEndPoint listener in listeners)
                {
                    if (listener.Port == port)
                    {
                        portInUse = true;
                        break;
                    }
                }

                if (!portInUse)
                {
                    return;
                }

                logger.LogWarning("端口 {Port} 被占用，等待释放...", port);
                await Task.Delay(1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "检查端口状态时发生错误");
                await Task.Delay(1000, cancellationToken);
            }
        }

        throw new InvalidOperationException($"端口 {port} 在 {timeout.TotalSeconds} 秒内未能释放");
    }
}
#endif