using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using NitroxModel.Logger;
using NitroxNetwork.BelowZero.Core;
using NitroxEvents.BelowZero.Core;

namespace NitroxServer.BelowZero.Core
{
    /// <summary>
    /// Below Zero网络服务器 - 直接套用Below Zero的网络服务架构
    /// </summary>
    public class BelowZeroNetworkServer : INetEventListener
    {
        private NetManager netManager;
        private readonly ConcurrentDictionary<string, NetPeer> playerPeers = new();
        private readonly ConcurrentDictionary<NetPeer, string> peerToPlayerId = new();
        private bool isRunning = false;

        /// <summary>
        /// 服务器是否运行中
        /// </summary>
        public bool IsRunning => isRunning;

        /// <summary>
        /// 当前连接的玩家数量
        /// </summary>
        public int ConnectedPlayerCount => playerPeers.Count;

        /// <summary>
        /// 启动网络服务器
        /// </summary>
        public async Task<bool> StartAsync(int port, int maxConnections)
        {
            try
            {
                netManager = new NetManager(this);
                netManager.UpdateTime = 15; // 15ms更新间隔
                netManager.DisconnectTimeout = 30000; // 30秒超时
                
                bool started = netManager.Start(port);
                
                if (started)
                {
                    isRunning = true;
                    Log.Info($"Below Zero网络服务器启动成功，端口: {port}，最大连接数: {maxConnections}");
                    return true;
                }
                else
                {
                    Log.Error($"Below Zero网络服务器启动失败，端口: {port}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"启动网络服务器时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止网络服务器
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                if (!isRunning)
                    return;

                Log.Info("正在停止Below Zero网络服务器...");

                // 断开所有连接
                foreach (var peer in playerPeers.Values)
                {
                    peer.Disconnect();
                }

                netManager?.Stop();
                playerPeers.Clear();
                peerToPlayerId.Clear();
                
                isRunning = false;
                
                Log.Info("Below Zero网络服务器已停止");
            }
            catch (Exception ex)
            {
                Log.Error($"停止网络服务器时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新网络服务器
        /// </summary>
        public void Update()
        {
            try
            {
                if (isRunning && netManager != null)
                {
                    netManager.PollEvents();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"网络服务器更新时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送数据包给指定玩家
        /// </summary>
        public bool SendPacketToPlayer(string playerId, BelowZeroNetworkPacket packet)
        {
            try
            {
                if (!playerPeers.TryGetValue(playerId, out var peer))
                {
                    Log.Warn($"无法找到玩家连接: {playerId}");
                    return false;
                }

                if (peer.ConnectionState != ConnectionState.Connected)
                {
                    Log.Warn($"玩家 {playerId} 连接状态异常: {peer.ConnectionState}");
                    return false;
                }

                var data = BelowZeroNetworkTools.SerializePacket(packet);
                var deliveryMethod = BelowZeroNetworkTools.ToLiteNetLibDeliveryMethod(packet.DeliveryMethod);
                var channelId = (byte)packet.Channel;

                peer.Send(data, channelId, deliveryMethod);
                
                BelowZeroServer.Statistics.PacketsSent++;
                Log.Debug($"发送数据包给玩家 {playerId}: {packet.PacketType}");
                
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"发送数据包给玩家 {playerId} 时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 广播数据包给所有玩家
        /// </summary>
        public void BroadcastPacket(BelowZeroNetworkPacket packet, string excludePlayerId = null)
        {
            foreach (var kvp in playerPeers)
            {
                var playerId = kvp.Key;
                if (playerId != excludePlayerId)
                {
                    SendPacketToPlayer(playerId, packet);
                }
            }
        }

        /// <summary>
        /// 断开指定玩家
        /// </summary>
        public async Task<bool> DisconnectPlayerAsync(string playerId)
        {
            try
            {
                if (playerPeers.TryGetValue(playerId, out var peer))
                {
                    peer.Disconnect();
                    
                    // 等待断开完成
                    await Task.Delay(100);
                    
                    return true;
                }
                
                Log.Warn($"尝试断开不存在的玩家: {playerId}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"断开玩家 {playerId} 时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取玩家的网络延迟
        /// </summary>
        public int GetPlayerPing(string playerId)
        {
            if (playerPeers.TryGetValue(playerId, out var peer))
            {
                return peer.Ping;
            }
            return -1;
        }

        #region INetEventListener 实现

        public void OnPeerConnected(NetPeer peer)
        {
            try
            {
                Log.Info("新的Below Zero连接已建立");
                
                // 这里可以进行更多的连接验证和设置
                var playerId = Guid.NewGuid().ToString();
                
                // 暂时使用IP作为标识，实际应该等待客户端发送认证信息
                playerPeers.TryAdd(playerId, peer);
                peerToPlayerId.TryAdd(peer, playerId);
                
                Log.Debug($"分配Below Zero玩家ID: {playerId}");
            }
            catch (Exception ex)
            {
                Log.Error($"处理玩家连接时发生异常: {ex.Message}");
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            try
            {
                if (peerToPlayerId.TryRemove(peer, out var playerId))
                {
                    playerPeers.TryRemove(playerId, out _);
                    
                    // 从服务器移除玩家
                    BelowZeroServer.RemovePlayer(playerId);
                    
                    Log.Info($"Below Zero玩家断开连接, 原因: {disconnectInfo.Reason}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"处理玩家断开时发生异常: {ex.Message}");
            }
        }

        public void OnNetworkError(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            Log.Error($"网络错误: {socketError} @ {endPoint}");
            BelowZeroServer.Statistics.NetworkErrors++;
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            try
            {
                if (!peerToPlayerId.TryGetValue(peer, out var playerId))
                {
                    Log.Warn("收到未知Below Zero连接的数据");
                    return;
                }

                var data = reader.GetRemainingBytes();
                BelowZeroServer.Statistics.PacketsReceived++;
                
                // 处理接收到的数据包
                ProcessReceivedPacket(playerId, data, channelNumber);
                
                Log.Debug($"接收数据包 from {playerId}: 通道 {channelNumber}, 大小: {data.Length}");
            }
            catch (Exception ex)
            {
                Log.Error($"处理接收数据时发生异常: {ex.Message}");
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Log.Debug($"接收到未连接消息: {messageType} from {remoteEndPoint}");
            
            // 这里可以处理服务器发现请求等
            if (messageType == UnconnectedMessageType.BasicMessage)
            {
                var message = reader.GetString();
                if (message == "BelowZeroServerDiscovery")
                {
                    // 响应服务器发现请求
                    var response = new NetDataWriter();
                    response.Put("BelowZeroServerInfo");
                    response.Put(BelowZeroServer.Config.ServerName);
                    response.Put(BelowZeroServer.ConnectedPlayers.Count);
                    response.Put(BelowZeroServer.Config.MaxPlayers);
                    
                    netManager.SendUnconnectedMessage(response, remoteEndPoint);
                }
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            if (peerToPlayerId.TryGetValue(peer, out var playerId))
            {
                Log.Debug($"延迟更新 {playerId}: {latency}ms");
            }
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            try
            {
                Log.Info($"连接请求: {request.RemoteEndPoint}");
                
                // 检查服务器是否已满
                if (ConnectedPlayerCount >= BelowZeroServer.Config.MaxPlayers)
                {
                    request.Reject();
                    Log.Info($"拒绝连接（服务器已满）: {request.RemoteEndPoint}");
                    return;
                }
                
                // 这里可以添加更多的验证逻辑（如密码验证等）
                var connectionData = request.Data;
                var clientInfo = connectionData.GetString();
                
                if (clientInfo.StartsWith("BelowZero_"))
                {
                    request.Accept();
                    Log.Info($"接受连接: {request.RemoteEndPoint}");
                }
                else
                {
                    request.Reject();
                    Log.Info($"拒绝连接（无效客户端）: {request.RemoteEndPoint}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"处理连接请求时发生异常: {ex.Message}");
                request.Reject();
            }
        }

        #endregion

        /// <summary>
        /// 处理接收到的数据包
        /// </summary>
        private void ProcessReceivedPacket(string playerId, byte[] data, byte channelNumber)
        {
            try
            {
                // 这里应该根据数据包类型进行反序列化和处理
                // 暂时创建一个通用事件
                var networkEvent = new BelowZeroNetworkEvent(playerId, "PacketReceived", playerId, data);
                BelowZeroEventManager.TriggerEvent(networkEvent);
                
                // 根据通道类型进行不同的处理
                var channel = (NitroxModel.BelowZero.Enums.NetworkChannel)channelNumber;
                switch (channel)
                {
                    case NitroxModel.BelowZero.Enums.NetworkChannel.PlayerMovement:
                        // 处理玩家移动
                        break;
                    case NitroxModel.BelowZero.Enums.NetworkChannel.VehicleMovement:
                        // 处理载具移动
                        break;
                    case NitroxModel.BelowZero.Enums.NetworkChannel.Construction:
                        // 处理建造
                        break;
                    default:
                        // 默认处理
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"处理数据包时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            StopAsync().Wait();
            netManager = null;
        }
    }
}
