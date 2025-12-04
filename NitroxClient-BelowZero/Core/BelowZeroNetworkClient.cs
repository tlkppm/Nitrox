using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using NitroxModel.BelowZero.Enums;
using NitroxModel.Logger;
using NitroxNetwork.BelowZero.Core;
using NitroxEvents.BelowZero.Core;

namespace NitroxClient.BelowZero.Core
{
    /// <summary>
    /// 通用Below Zero网络数据包 - 用于暂时处理未知类型的数据包
    /// </summary>
    public class GenericBelowZeroPacket : BelowZeroNetworkPacket
    {
        public override string PacketType => "Generic";
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public override NetworkChannel Channel => NetworkChannel.Default;
    }

    /// <summary>
    /// Below Zero网络客户端 - 直接套用Below Zero的网络通信架构
    /// </summary>
    public class BelowZeroNetworkClient : INetEventListener
    {
        private NetManager netManager;
        private NetPeer serverPeer;
        private readonly ConcurrentQueue<BelowZeroNetworkPacket> outgoingPackets = new();
        private readonly ConcurrentQueue<BelowZeroNetworkPacket> incomingPackets = new();
        private readonly Timer networkTimer;
        
        /// <summary>
        /// 连接状态
        /// </summary>
        public bool IsConnected => serverPeer?.ConnectionState == ConnectionState.Connected;
        
        /// <summary>
        /// 网络统计信息
        /// </summary>
        public BelowZeroNetworkStatistics Statistics { get; private set; } = new();

        /// <summary>
        /// 玩家名称
        /// </summary>
        public string PlayerName { get; private set; } = string.Empty;

        public BelowZeroNetworkClient()
        {
            netManager = new NetManager(this);
            netManager.UpdateTime = 15; // 15ms更新间隔
            netManager.DisconnectTimeout = 30000; // 30秒超时
            
            // 网络处理定时器
            networkTimer = new Timer(ProcessNetworkPackets, null, 0, 50); // 每50ms处理一次
            
            Log.Debug("Below Zero网络客户端已创建");
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public bool Connect(string address, int port, string playerName)
        {
            try
            {
                PlayerName = playerName;
                
                netManager.Start();
                serverPeer = netManager.Connect(address, port, "BelowZero_" + playerName);
                
                if (serverPeer != null)
                {
                    Log.Info($"正在连接到Below Zero服务器: {address}:{port}");
                    
                    // 等待连接建立（最多5秒）
                    var waitTime = 0;
                    while (serverPeer.ConnectionState == ConnectionState.Outgoing && waitTime < 5000)
                    {
                        netManager.PollEvents();
                        Thread.Sleep(100);
                        waitTime += 100;
                    }
                    
                    bool connected = serverPeer.ConnectionState == ConnectionState.Connected;
                    if (connected)
                    {
                        Statistics.ConnectionEstablished = DateTime.UtcNow;
                        Log.Info("成功连接到Below Zero服务器");
                    }
                    else
                    {
                        Log.Error($"连接失败，状态: {serverPeer.ConnectionState}");
                    }
                    
                    return connected;
                }
                
                Log.Error("无法创建到服务器的连接");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"连接服务器时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (serverPeer?.ConnectionState == ConnectionState.Connected)
                {
                    serverPeer.Disconnect();
                    Log.Info("已断开Below Zero服务器连接");
                }
                
                netManager?.Stop();
                Statistics.ConnectionClosed = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Log.Error($"断开连接时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送数据包
        /// </summary>
        public void SendPacket(BelowZeroNetworkPacket packet)
        {
            if (packet == null)
            {
                Log.Warn("尝试发送空数据包");
                return;
            }

            if (!BelowZeroNetworkTools.ValidatePacket(packet))
            {
                Log.Warn($"数据包验证失败: {packet.PacketType}");
                return;
            }

            packet.SenderId = PlayerName;
            outgoingPackets.Enqueue(packet);
            Statistics.PacketsSent++;
        }

        /// <summary>
        /// 获取接收到的数据包
        /// </summary>
        public bool TryGetIncomingPacket(out BelowZeroNetworkPacket packet)
        {
            return incomingPackets.TryDequeue(out packet);
        }

        /// <summary>
        /// 更新网络客户端
        /// </summary>
        public void Update()
        {
            try
            {
                netManager?.PollEvents();
                ProcessIncomingPackets();
            }
            catch (Exception ex)
            {
                Log.Error($"网络更新时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取网络状态
        /// </summary>
        public string GetStatus()
        {
            var status = $"Below Zero Network Client:\n";
            status += $"Connected: {IsConnected}\n";
            status += $"Player: {PlayerName}\n";
            status += $"Packets Sent: {Statistics.PacketsSent}\n";
            status += $"Packets Received: {Statistics.PacketsReceived}\n";
            status += $"Connection Time: {Statistics.GetConnectionDuration()}\n";
            
            if (serverPeer != null)
            {
                status += $"Server: Connected\n";
                status += $"Ping: {serverPeer.Ping}ms\n";
            }
            
            return status;
        }

        /// <summary>
        /// 处理网络数据包
        /// </summary>
        private void ProcessNetworkPackets(object state)
        {
            // 处理发送队列
            while (outgoingPackets.TryDequeue(out var outgoingPacket))
            {
                try
                {
                    SendPacketToServer(outgoingPacket);
                }
                catch (Exception ex)
                {
                    Log.Error($"发送数据包失败: {outgoingPacket.PacketType}, 错误: {ex.Message}");
                    Statistics.PacketErrors++;
                }
            }
        }

        /// <summary>
        /// 发送数据包到服务器
        /// </summary>
        private void SendPacketToServer(BelowZeroNetworkPacket packet)
        {
            if (serverPeer?.ConnectionState != ConnectionState.Connected)
            {
                Log.Warn($"无法发送数据包，连接状态: {serverPeer?.ConnectionState}");
                return;
            }

            var data = BelowZeroNetworkTools.SerializePacket(packet);
            var deliveryMethod = BelowZeroNetworkTools.ToLiteNetLibDeliveryMethod(packet.DeliveryMethod);
            var channelId = (byte)packet.Channel;

            serverPeer.Send(data, channelId, deliveryMethod);
            Log.Debug($"发送数据包: {packet.PacketType} (通道: {packet.Channel})");
        }

        /// <summary>
        /// 处理接收到的数据包
        /// </summary>
        private void ProcessIncomingPackets()
        {
            while (incomingPackets.TryDequeue(out var packet))
            {
                try
                {
                    // 触发数据包接收事件
                    BelowZeroEventManager.TriggerEvent(new BelowZeroNetworkEvent(
                        BelowZeroClient.ClientId, 
                        "PacketReceived", 
                        packet.SenderId, 
                        packet));
                }
                catch (Exception ex)
                {
                    Log.Error($"处理接收数据包失败: {packet.PacketType}, 错误: {ex.Message}");
                }
            }
        }

        #region INetEventListener 实现

        public void OnPeerConnected(NetPeer peer)
        {
            Log.Info($"已连接到Below Zero服务器");
            BelowZeroEventManager.TriggerEvent(new BelowZeroNetworkEvent(BelowZeroClient.ClientId, "Connected"));
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Log.Info($"已断开Below Zero服务器连接: {disconnectInfo.Reason}");
            Statistics.ConnectionClosed = DateTime.UtcNow;
            BelowZeroEventManager.TriggerEvent(new BelowZeroNetworkEvent(BelowZeroClient.ClientId, "Disconnected"));
        }

        public void OnNetworkError(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            Log.Error($"Below Zero网络错误: {socketError} @ {endPoint}");
            Statistics.NetworkErrors++;
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            try
            {
                var data = reader.GetRemainingBytes();
                var networkChannel = (NetworkChannel)channelNumber;
                
                // 这里需要根据实际的数据包类型进行反序列化
                // 暂时创建一个通用的网络数据包
                var packet = new GenericBelowZeroPacket
                {
                    SenderId = peer.Id.ToString(),
                    Data = data
                };
                
                incomingPackets.Enqueue(packet);
                Statistics.PacketsReceived++;
                
                Log.Debug($"接收数据包: 通道 {networkChannel}, 大小: {data.Length}");
            }
            catch (Exception ex)
            {
                Log.Error($"处理接收数据时发生异常: {ex.Message}");
                Statistics.PacketErrors++;
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Log.Debug($"接收到未连接消息: {messageType} from {remoteEndPoint}");
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            Statistics.LastPing = latency;
            Log.Debug($"网络延迟更新: {latency}ms");
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            Log.Debug($"收到连接请求: {request.RemoteEndPoint}");
        }

        #endregion

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            networkTimer?.Dispose();
            Disconnect();
            netManager = null;
        }
    }

    /// <summary>
    /// Below Zero网络统计信息
    /// </summary>
    public class BelowZeroNetworkStatistics
    {
        public DateTime ConnectionEstablished { get; set; }
        public DateTime ConnectionClosed { get; set; }
        public long PacketsSent { get; set; }
        public long PacketsReceived { get; set; }
        public long PacketErrors { get; set; }
        public long NetworkErrors { get; set; }
        public int LastPing { get; set; }

        public TimeSpan GetConnectionDuration()
        {
            if (ConnectionEstablished == default)
                return TimeSpan.Zero;
                
            var endTime = ConnectionClosed == default ? DateTime.UtcNow : ConnectionClosed;
            return endTime - ConnectionEstablished;
        }

        public void Reset()
        {
            ConnectionEstablished = default;
            ConnectionClosed = default;
            PacketsSent = 0;
            PacketsReceived = 0;
            PacketErrors = 0;
            NetworkErrors = 0;
            LastPing = 0;
        }
    }
}
