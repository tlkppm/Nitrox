using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NitroxModel.BelowZero.Features;
using NitroxModel.Logger;
using NitroxEvents.BelowZero.Core;
using NitroxNetwork.BelowZero.Core;
using NitroxServer.BelowZero.Managers;

namespace NitroxServer.BelowZero.Core
{
    /// <summary>
    /// Below Zero服务器主类 - 直接套用Below Zero的服务器架构
    /// </summary>
    public static class BelowZeroServer
    {
        private static bool isRunning = false;
        private static BelowZeroNetworkServer networkServer;
        private static Timer serverUpdateTimer;
        private static readonly object serverLock = new();
        
        /// <summary>
        /// 服务器ID
        /// </summary>
        public static string ServerId { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 服务器配置
        /// </summary>
        public static BelowZeroServerConfig Config { get; private set; } = new();

        /// <summary>
        /// 已连接的玩家
        /// </summary>
        public static ConcurrentDictionary<string, BelowZeroPlayer> ConnectedPlayers { get; private set; } = new();

        /// <summary>
        /// 服务器统计信息
        /// </summary>
        public static BelowZeroServerStatistics Statistics { get; private set; } = new();

        /// <summary>
        /// 世界管理器
        /// </summary>
        public static BelowZeroWorldManager WorldManager { get; private set; }

        /// <summary>
        /// 玩家管理器
        /// </summary>
        public static BelowZeroPlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// 载具管理器
        /// </summary>
        public static Managers.BelowZeroVehicleManager VehicleManager { get; private set; }

        /// <summary>
        /// 天气管理器
        /// </summary>
        public static Managers.BelowZeroWeatherManager WeatherManager { get; private set; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public static bool IsRunning => isRunning;

        /// <summary>
        /// 启动Below Zero服务器
        /// </summary>
        public static async Task<bool> StartAsync(BelowZeroServerConfig config = null)
        {
            bool needToStart = false;
            BelowZeroNetworkServer tempNetworkServer = null;
            BelowZeroServerConfig tempConfig = null;
            
            lock (serverLock)
            {
                if (isRunning)
                {
                    Log.Warn("Below Zero服务器已经在运行");
                    return true;
                }

                Log.Info("正在启动Below Zero服务器...");
                
                // 应用配置
                tempConfig = config ?? new BelowZeroServerConfig();
                Config = tempConfig;
                
                // 初始化核心系统
                InitializeCoreServices();
                
                // 初始化管理器
                InitializeManagers();
                
                // 创建网络服务器
                tempNetworkServer = new BelowZeroNetworkServer();
                networkServer = tempNetworkServer;
                needToStart = true;
            }

            if (needToStart)
            {
                try
                {
                    // 在lock外部启动网络服务器
                    bool networkStarted = await tempNetworkServer.StartAsync(tempConfig.Port, tempConfig.MaxPlayers);
                    
                    if (!networkStarted)
                    {
                        Log.Error("网络服务器启动失败");
                        return false;
                    }
                    
                    // 启动服务器更新循环
                    StartUpdateLoop();
                    
                    isRunning = true;
                    Statistics.ServerStartTime = DateTime.UtcNow;
                    
                    Log.Info($"Below Zero服务器启动成功，端口: {Config.Port}，最大玩家数: {Config.MaxPlayers}");
                    
                    // 触发服务器启动事件
                    BelowZeroEventManager.TriggerEvent(new BelowZeroNetworkEvent(ServerId, "ServerStarted"));
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error($"启动Below Zero服务器失败: {ex.Message}");
                    return false;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 停止Below Zero服务器
        /// </summary>
        public static async Task StopAsync()
        {
            BelowZeroNetworkServer tempNetworkServer = null;
            bool needToStop = false;
            
            lock (serverLock)
            {
                if (!isRunning)
                {
                    Log.Warn("Below Zero服务器未运行");
                    return;
                }

                Log.Info("正在停止Below Zero服务器...");
                
                // 停止更新循环
                StopUpdateLoop();
                
                tempNetworkServer = networkServer;
                needToStop = true;
            }
            
            if (needToStop)
            {
                try
                {
                    // 断开所有玩家
                    await DisconnectAllPlayersAsync();
                    
                    // 停止网络服务器
                    await tempNetworkServer?.StopAsync();
                    
                    // 清理管理器
                    CleanupManagers();
                    
                    // 清理核心服务
                    CleanupCoreServices();
                    
                    isRunning = false;
                    Statistics.ServerStopTime = DateTime.UtcNow;
                    
                    Log.Info("Below Zero服务器已停止");
                    
                    // 触发服务器停止事件
                    BelowZeroEventManager.TriggerEvent(new BelowZeroNetworkEvent(ServerId, "ServerStopped"));
                }
                catch (Exception ex)
                {
                    Log.Error($"停止Below Zero服务器时发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 添加玩家
        /// </summary>
        public static bool AddPlayer(BelowZeroPlayer player)
        {
            if (ConnectedPlayers.Count >= Config.MaxPlayers)
            {
                Log.Warn($"服务器已满，无法添加玩家: {player.Name}");
                return false;
            }

            if (ConnectedPlayers.TryAdd(player.Id, player))
            {
                Statistics.TotalPlayersConnected++;
                Log.Info($"玩家已连接: {player.Name} (ID: {player.Id})");
                
                // 触发玩家连接事件
                BelowZeroEventManager.TriggerEvent(new BelowZeroNetworkEvent(player.Id, "PlayerConnected", player.Name));
                
                return true;
            }

            Log.Error($"添加玩家失败: {player.Name}");
            return false;
        }

        /// <summary>
        /// 移除玩家
        /// </summary>
        public static bool RemovePlayer(string playerId)
        {
            if (ConnectedPlayers.TryRemove(playerId, out var player))
            {
                Log.Info($"玩家已断开: {player.Name} (ID: {playerId})");
                
                // 触发玩家断开事件
                BelowZeroEventManager.TriggerEvent(new BelowZeroNetworkEvent(playerId, "PlayerDisconnected", player.Name));
                
                return true;
            }

            Log.Warn($"尝试移除不存在的玩家: {playerId}");
            return false;
        }

        /// <summary>
        /// 广播数据包到所有玩家
        /// </summary>
        public static void BroadcastPacket(BelowZeroNetworkPacket packet, string excludePlayerId = null)
        {
            var targetPlayers = ConnectedPlayers.Values
                .Where(p => p.Id != excludePlayerId)
                .ToList();

            foreach (var player in targetPlayers)
            {
                try
                {
                    networkServer?.SendPacketToPlayer(player.Id, packet);
                    Statistics.PacketsSent++;
                }
                catch (Exception ex)
                {
                    Log.Error($"向玩家 {player.Name} 发送数据包失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取服务器状态
        /// </summary>
        public static string GetServerStatus()
        {
            var uptime = isRunning ? DateTime.UtcNow - Statistics.ServerStartTime : TimeSpan.Zero;
            
            var status = $"Below Zero Server Status:\n";
            status += $"Server ID: {ServerId}\n";
            status += $"Running: {isRunning}\n";
            status += $"Port: {Config.Port}\n";
            status += $"Players: {ConnectedPlayers.Count}/{Config.MaxPlayers}\n";
            status += $"Uptime: {uptime}\n";
            status += $"Total Connections: {Statistics.TotalPlayersConnected}\n";
            status += $"Packets Sent: {Statistics.PacketsSent}\n";
            status += $"Packets Received: {Statistics.PacketsReceived}\n";
            
            return status;
        }

        /// <summary>
        /// 初始化核心服务
        /// </summary>
        private static void InitializeCoreServices()
        {
            BelowZeroNetwork.Initialize();
            BelowZeroEventManager.Initialize();
            BelowZeroWorld.Initialize();
            
            Log.Debug("Below Zero服务器核心服务初始化完成");
        }

        /// <summary>
        /// 初始化管理器
        /// </summary>
        private static void InitializeManagers()
        {
            WorldManager = new BelowZeroWorldManager();
            PlayerManager = new BelowZeroPlayerManager();
            VehicleManager = new Managers.BelowZeroVehicleManager();
            WeatherManager = new Managers.BelowZeroWeatherManager();
            
            Log.Debug("Below Zero服务器管理器初始化完成");
        }

        /// <summary>
        /// 启动更新循环
        /// </summary>
        private static void StartUpdateLoop()
        {
            serverUpdateTimer = new Timer(ServerUpdate, null, 0, Config.UpdateIntervalMs);
            Log.Debug($"服务器更新循环已启动，间隔: {Config.UpdateIntervalMs}ms");
        }

        /// <summary>
        /// 停止更新循环
        /// </summary>
        private static void StopUpdateLoop()
        {
            serverUpdateTimer?.Dispose();
            serverUpdateTimer = null;
            Log.Debug("服务器更新循环已停止");
        }

        /// <summary>
        /// 服务器更新方法
        /// </summary>
        private static void ServerUpdate(object state)
        {
            try
            {
                if (!isRunning) return;

                // 更新网络服务器
                networkServer?.Update();
                
                // 更新管理器
                WorldManager?.Update();
                PlayerManager?.Update();
                VehicleManager?.Update();
                WeatherManager?.Update();
                
                // 更新统计信息
                Statistics.LastUpdateTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Log.Error($"服务器更新时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开所有玩家
        /// </summary>
        private static async Task DisconnectAllPlayersAsync()
        {
            var players = ConnectedPlayers.Values.ToList();
            
            foreach (var player in players)
            {
                try
                {
                    await networkServer?.DisconnectPlayerAsync(player.Id);
                    RemovePlayer(player.Id);
                }
                catch (Exception ex)
                {
                    Log.Error($"断开玩家 {player.Name} 时发生异常: {ex.Message}");
                }
            }
            
            ConnectedPlayers.Clear();
        }

        /// <summary>
        /// 清理管理器
        /// </summary>
        private static void CleanupManagers()
        {
            WorldManager?.Cleanup();
            PlayerManager?.Cleanup();
            VehicleManager?.Cleanup();
            WeatherManager?.Cleanup();
            
            WorldManager = null;
            PlayerManager = null;
            VehicleManager = null;
            WeatherManager = null;
            
            Log.Debug("Below Zero服务器管理器清理完成");
        }

        /// <summary>
        /// 清理核心服务
        /// </summary>
        private static void CleanupCoreServices()
        {
            BelowZeroNetwork.Cleanup();
            BelowZeroEventManager.Cleanup();
            BelowZeroWorld.Cleanup();
            
            Log.Debug("Below Zero服务器核心服务清理完成");
        }
    }

    /// <summary>
    /// Below Zero玩家信息
    /// </summary>
    public class BelowZeroPlayer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime ConnectedTime { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public string CurrentBiome { get; set; }
        public float Temperature { get; set; }
        public bool IsInVehicle { get; set; }
        public string VehicleId { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new();

        public BelowZeroPlayer(string id, string name)
        {
            Id = id;
            Name = name;
            ConnectedTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Below Zero服务器配置
    /// </summary>
    public class BelowZeroServerConfig
    {
        public int Port { get; set; } = 11000;
        public int MaxPlayers { get; set; } = 10;
        public int UpdateIntervalMs { get; set; } = 50; // 50ms更新间隔
        public bool EnableWeatherSync { get; set; } = true;
        public bool EnableIceSync { get; set; } = true;
        public bool EnableVehicleSync { get; set; } = true;
        public bool EnableBiomeSync { get; set; } = true;
        public string ServerName { get; set; } = "Below Zero Server";
        public string Password { get; set; } = string.Empty;
        public TimeSpan PlayerTimeout { get; set; } = TimeSpan.FromMinutes(5);

        public void LoadFromFile(string configPath)
        {
            Log.Debug($"加载Below Zero服务器配置: {configPath}");
        }

        public void SaveToFile(string configPath)
        {
            Log.Debug($"保存Below Zero服务器配置: {configPath}");
        }
    }

    /// <summary>
    /// Below Zero服务器统计信息
    /// </summary>
    public class BelowZeroServerStatistics
    {
        public DateTime ServerStartTime { get; set; }
        public DateTime ServerStopTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public long TotalPlayersConnected { get; set; }
        public long PacketsSent { get; set; }
        public long PacketsReceived { get; set; }
        public long NetworkErrors { get; set; }

        public TimeSpan GetUptime()
        {
            if (ServerStartTime == default)
                return TimeSpan.Zero;
                
            var endTime = ServerStopTime == default ? DateTime.UtcNow : ServerStopTime;
            return endTime - ServerStartTime;
        }

        public void Reset()
        {
            ServerStartTime = default;
            ServerStopTime = default;
            LastUpdateTime = default;
            TotalPlayersConnected = 0;
            PacketsSent = 0;
            PacketsReceived = 0;
            NetworkErrors = 0;
        }
    }
}
