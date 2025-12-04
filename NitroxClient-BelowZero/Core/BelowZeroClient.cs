using System;
using System.Collections.Generic;
using HarmonyLib;
using NitroxModel.BelowZero.Features;
using NitroxModel.Logger;
using NitroxEvents.BelowZero.Core;
using NitroxNetwork.BelowZero.Core;

namespace NitroxClient.BelowZero.Core
{
    /// <summary>
    /// Below Zero客户端主类 - 直接套用Below Zero的客户端架构
    /// </summary>
    public static class BelowZeroClient
    {
        private static bool isInitialized = false;
        private static Harmony harmonyInstance;
        
        /// <summary>
        /// 客户端ID
        /// </summary>
        public static string ClientId { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 是否已连接到服务器
        /// </summary>
        public static bool IsConnected { get; private set; } = false;

        /// <summary>
        /// 当前会话信息
        /// </summary>
        public static BelowZeroClientSession Session { get; private set; } = new();

        /// <summary>
        /// 客户端配置
        /// </summary>
        public static BelowZeroClientConfig Config { get; private set; } = new();

        /// <summary>
        /// 网络客户端
        /// </summary>
        public static BelowZeroNetworkClient NetworkClient { get; private set; }

        /// <summary>
        /// 初始化Below Zero客户端
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized)
            {
                Log.Warn("Below Zero客户端已经初始化");
                return;
            }

            Log.Info("正在初始化Below Zero客户端...");

            try
            {
                // 初始化网络系统
                BelowZeroNetwork.Initialize();
                
                // 初始化事件系统
                BelowZeroEventManager.Initialize();
                
                // 初始化网络工具
                RegisterNetworkPackets();
                
                // 初始化Harmony补丁
                InitializeHarmonyPatches();
                
                // 初始化网络客户端
                NetworkClient = new BelowZeroNetworkClient();
                
                // 初始化世界系统
                BelowZeroWorld.Initialize();
                
                isInitialized = true;
                Log.Info("Below Zero客户端初始化完成");
                
                // 触发初始化事件
                BelowZeroEventManager.TriggerEvent(new BelowZeroNetworkEvent(ClientId, "ClientInitialized"));
            }
            catch (Exception ex)
            {
                Log.Error($"Below Zero客户端初始化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 连接到Below Zero服务器
        /// </summary>
        public static bool ConnectToServer(string serverAddress, int port, string playerName)
        {
            try
            {
                Log.Info($"正在连接到Below Zero服务器: {serverAddress}:{port}");
                
                if (NetworkClient == null)
                {
                    Log.Error("网络客户端未初始化");
                    return false;
                }

                bool success = NetworkClient.Connect(serverAddress, port, playerName);
                
                if (success)
                {
                    IsConnected = true;
                    Session.StartSession(playerName, serverAddress, port);
                    
                    Log.Info("成功连接到Below Zero服务器");
                    BelowZeroEventManager.TriggerEvent(new BelowZeroNetworkEvent(ClientId, "Connected", playerName));
                }
                else
                {
                    Log.Error("连接到Below Zero服务器失败");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log.Error($"连接服务器时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 断开服务器连接
        /// </summary>
        public static void Disconnect()
        {
            try
            {
                Log.Info("正在断开Below Zero服务器连接...");
                
                NetworkClient?.Disconnect();
                IsConnected = false;
                Session.EndSession();
                
                Log.Info("已断开Below Zero服务器连接");
                BelowZeroEventManager.TriggerEvent(new BelowZeroNetworkEvent(ClientId, "Disconnected"));
            }
            catch (Exception ex)
            {
                Log.Error($"断开连接时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理客户端资源
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                Log.Info("正在清理Below Zero客户端资源...");
                
                Disconnect();
                
                // 清理Harmony补丁
                CleanupHarmonyPatches();
                
                // 清理网络系统
                BelowZeroNetwork.Cleanup();
                
                // 清理事件系统
                BelowZeroEventManager.Cleanup();
                
                // 清理世界系统
                BelowZeroWorld.Cleanup();
                
                NetworkClient = null;
                isInitialized = false;
                
                Log.Info("Below Zero客户端资源清理完成");
            }
            catch (Exception ex)
            {
                Log.Error($"清理客户端资源时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新客户端（每帧调用）
        /// </summary>
        public static void Update()
        {
            try
            {
                if (!isInitialized || !IsConnected)
                    return;

                NetworkClient?.Update();
                Session.Update();
            }
            catch (Exception ex)
            {
                Log.Error($"客户端更新时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册网络数据包
        /// </summary>
        private static void RegisterNetworkPackets()
        {
            // 注册Below Zero特有的数据包类型
            BelowZeroNetworkTools.RegisterPacketType<NitroxNetwork.BelowZero.Packets.BelowZeroSeatruckPacket>();
            BelowZeroNetworkTools.RegisterPacketType<NitroxNetwork.BelowZero.Packets.BelowZeroWeatherPacket>();
            BelowZeroNetworkTools.RegisterPacketType<NitroxNetwork.BelowZero.Packets.BelowZeroIcePacket>();
            
            Log.Debug("Below Zero网络数据包注册完成");
        }

        /// <summary>
        /// 初始化Harmony补丁
        /// </summary>
        private static void InitializeHarmonyPatches()
        {
            try
            {
                harmonyInstance = new Harmony("com.nitrox.belowzero");
                
                // 自动应用所有Below Zero补丁
                harmonyInstance.PatchAll(typeof(BelowZeroClient).Assembly);
                
                Log.Info("Below Zero Harmony补丁应用完成");
            }
            catch (Exception ex)
            {
                Log.Error($"应用Harmony补丁失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 清理Harmony补丁
        /// </summary>
        private static void CleanupHarmonyPatches()
        {
            try
            {
                harmonyInstance?.UnpatchAll("com.nitrox.belowzero");
                Log.Info("Below Zero Harmony补丁清理完成");
            }
            catch (Exception ex)
            {
                Log.Error($"清理Harmony补丁失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取客户端状态
        /// </summary>
        public static string GetStatus()
        {
            var status = $"Below Zero Client Status:\n";
            status += $"Client ID: {ClientId}\n";
            status += $"Initialized: {isInitialized}\n";
            status += $"Connected: {IsConnected}\n";
            status += $"Session Active: {Session.IsActive}\n";
            
            if (NetworkClient != null)
            {
                status += $"Network Status: {NetworkClient.GetStatus()}\n";
            }
            
            return status;
        }
    }

    /// <summary>
    /// Below Zero客户端会话信息
    /// </summary>
    public class BelowZeroClientSession
    {
        public string PlayerName { get; private set; } = string.Empty;
        public string ServerAddress { get; private set; } = string.Empty;
        public int ServerPort { get; private set; } = 0;
        public DateTime SessionStartTime { get; private set; }
        public bool IsActive { get; private set; } = false;
        public TimeSpan SessionDuration => IsActive ? DateTime.UtcNow - SessionStartTime : TimeSpan.Zero;

        public void StartSession(string playerName, string serverAddress, int port)
        {
            PlayerName = playerName;
            ServerAddress = serverAddress;
            ServerPort = port;
            SessionStartTime = DateTime.UtcNow;
            IsActive = true;
            
            Log.Info($"Below Zero会话已开始: {playerName} @ {serverAddress}:{port}");
        }

        public void EndSession()
        {
            IsActive = false;
            Log.Info($"Below Zero会话已结束，持续时间: {SessionDuration}");
        }

        public void Update()
        {
            // 会话更新逻辑（心跳等）
        }
    }

    /// <summary>
    /// Below Zero客户端配置
    /// </summary>
    public class BelowZeroClientConfig
    {
        public bool EnableSeatruckSync { get; set; } = true;
        public bool EnableWeatherSync { get; set; } = true;
        public bool EnableIceSync { get; set; } = true;
        public bool EnableTemperatureSync { get; set; } = true;
        public bool EnableBiomeSync { get; set; } = true;
        public float UpdateFrequency { get; set; } = 0.1f; // 每0.1秒更新一次
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public void LoadFromFile(string configPath)
        {
            // 从文件加载配置的实现
            Log.Debug($"加载Below Zero客户端配置: {configPath}");
        }

        public void SaveToFile(string configPath)
        {
            // 保存配置到文件的实现
            Log.Debug($"保存Below Zero客户端配置: {configPath}");
        }
    }
}
