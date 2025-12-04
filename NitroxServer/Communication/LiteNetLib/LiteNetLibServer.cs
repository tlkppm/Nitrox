using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Mono.Nat;
using NitroxModel.Helper;
using NitroxModel.Packets;
using NitroxModel.Serialization;
using NitroxServer.Communication.Packets;
using NitroxServer.GameLogic;
using NitroxServer.GameLogic.Entities;

namespace NitroxServer.Communication.LiteNetLib;

public class LiteNetLibServer : NitroxServer
{
    private readonly EventBasedNetListener listener;
    private readonly NetManager server;

    public LiteNetLibServer(PacketHandler packetHandler, PlayerManager playerManager, EntitySimulation entitySimulation, SubnauticaServerConfig serverConfig) : base(packetHandler, playerManager, entitySimulation, serverConfig)
    {
        listener = new EventBasedNetListener();
        server = new NetManager(listener);
    }

    public override bool Start(CancellationToken ct = default)
    {
        Log.Info($"[网络服务] 开始启动LiteNetLib服务器 | 端口: {portNumber} | 最大连接: {maxConnections}");
        
        listener.PeerConnectedEvent += PeerConnected;
        listener.PeerDisconnectedEvent += PeerDisconnected;
        listener.NetworkReceiveEvent += NetworkDataReceived;
        listener.ConnectionRequestEvent += OnConnectionRequest;
        Log.Debug("[网络服务] 网络事件监听器已注册");

        server.ChannelsCount = (byte)typeof(Packet.UdpChannelId).GetEnumValues().Length;
        server.BroadcastReceiveEnabled = true;
        server.UnconnectedMessagesEnabled = true;
        server.UpdateTime = 15;
        server.UnsyncedEvents = true;
        Log.Debug($"[网络服务] 网络配置完成 - 通道数: {server.ChannelsCount}");
        
#if DEBUG
        server.DisconnectTimeout = 300000; //Disables Timeout (for 5 min) for debug purpose (like if you jump though the server code)
        Log.Debug("[网络服务] 调试模式 - 禁用超时");
#endif

        Log.Debug($"[网络服务] 尝试启动网络监听器在端口 {portNumber}");
        if (!server.Start(portNumber))
        {
            Log.Error($"[网络服务] ❌ 无法启动网络监听器在端口 {portNumber} - 端口可能被占用");
            return false;
        }
        
        Log.Info($"[网络服务] ✅ 网络监听器启动成功 | 监听端口: {portNumber} UDP");
        
        if (useUpnpPortForwarding)
        {
            Log.Debug("[网络服务] 启用UPnP端口转发");
            _ = PortForwardAsync((ushort)portNumber, ct);
        }
        if (useLANBroadcast)
        {
            Log.Debug("[网络服务] 启用LAN广播服务");
            LANBroadcastServer.Start(ct);
        }

        Log.Info($"[网络服务] 服务器已准备接受连接 | 端口: {portNumber} | 最大玩家: {maxConnections}");
        return true;
    }

    private async Task PortForwardAsync(ushort port, CancellationToken ct = default)
    {
        if (await NatHelper.GetPortMappingAsync(port, Protocol.Udp, ct) != null)
        {
            Log.Info($"Port {port} UDP is already port forwarded");
            return;
        }

        NatHelper.ResultCodes mappingResult = await NatHelper.AddPortMappingAsync(port, Protocol.Udp, ct);
        if (!ct.IsCancellationRequested)
        {
            switch (mappingResult)
            {
                case NatHelper.ResultCodes.SUCCESS:
                    Log.Info($"Server port {port} UDP has been automatically opened on your router (port is closed when server closes)");
                    break;
                case NatHelper.ResultCodes.CONFLICT_IN_MAPPING_ENTRY:
                    Log.Warn($"Port forward for {port} UDP failed. It appears to already be port forwarded or it conflicts with another port forward rule.");
                    break;
                case NatHelper.ResultCodes.UNKNOWN_ERROR:
                    Log.Warn($"Failed to port forward {port} UDP through UPnP. If using Hamachi or you've manually port-forwarded, please disregard this warning. To disable this feature you can go into the server settings.");
                    break;
            }
        }
    }

    public override void Stop()
    {
        if (!server.IsRunning)
        {
            return;
        }

        playerManager.SendPacketToAllPlayers(new ServerStopped());
        // We want every player to receive this packet
        Thread.Sleep(500);
        server.Stop();
        if (useUpnpPortForwarding)
        {
            if (NatHelper.DeletePortMappingAsync((ushort)portNumber, Protocol.Udp, CancellationToken.None).GetAwaiter().GetResult())
            {
                Log.Debug($"Port forward rule removed for {portNumber} UDP");
            }
            else
            {
                Log.Warn($"Failed to remove port forward rule {portNumber} UDP");
            }
        }
        if (useLANBroadcast)
        {
            LANBroadcastServer.Stop();
        }
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        string clientIP = request.RemoteEndPoint.ToString();
        
        if (server.ConnectedPeersCount < maxConnections)
        {
            Log.Info($"[网络连接] 接受来自 {clientIP} 的连接请求 | 当前连接数: {server.ConnectedPeersCount}/{maxConnections}");
            request.AcceptIfKey("nitrox");
        }
        else
        {
            Log.Warn($"[网络连接] 拒绝来自 {clientIP} 的连接请求 | 服务器已满: {server.ConnectedPeersCount}/{maxConnections}");
            request.Reject();
        }
    }

    private void PeerConnected(NetPeer peer)
    {
        LiteNetLibConnection connection = new(peer);
        lock (connectionsByRemoteIdentifier)
        {
            connectionsByRemoteIdentifier[peer.Id] = connection;
        }
        
        // 输出网络层连接成功信息
        Log.Info($"[网络层] 客户端已建立网络连接 | IP: {peer} | 连接ID: {peer.Id} | 当前网络连接数: {server.ConnectedPeersCount}");
         }

    private void PeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        // 输出网络层断开连接信息
        Log.Info($"[网络层] 客户端断开网络连接 | IP: {peer} | 连接ID: {peer.Id} | 断开原因: {disconnectInfo.Reason}");
        
        ClientDisconnected(GetConnection(peer.Id));
    }

    private void NetworkDataReceived(NetPeer peer, NetDataReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        int packetDataLength = reader.GetInt();
        byte[] packetData = ArrayPool<byte>.Shared.Rent(packetDataLength);
        try
        {
            reader.GetBytes(packetData, packetDataLength);
            Packet packet = Packet.Deserialize(packetData);
            INitroxConnection connection = GetConnection(peer.Id);
            
            // 添加数据包接收日志（过滤掉频繁的数据包类型以避免日志刷屏）
            string packetType = packet.GetType().Name;
            if (!IsFrequentPacketType(packetType))
            {
                Log.Info($"[数据包接收] 收到来自 {peer} 的数据包 | 类型: {packetType} | 大小: {packetDataLength} bytes | 通道: {channel}");
            }
            else
            {
                Log.Debug($"[数据包接收] {packetType} 来自 {peer}");
            }
            
            ProcessIncomingData(connection, packet);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(packetData, true);
        }
    }
    
    /// <summary>
    /// 判断是否为频繁发送的数据包类型（用于减少日志量）
    /// </summary>
    private static bool IsFrequentPacketType(string packetType)
    {
        return packetType switch
        {
            "PlayerMovement" => true,
            "PlayerStats" => true,
            "AnimationChangeEvent" => true,
            "ItemPosition" => true,
            "PingRequest" => false, // Ping请求我们需要看到
            "PingResponse" => false,
            _ => false
        };
    }

    private INitroxConnection GetConnection(int remoteIdentifier)
    {
        INitroxConnection connection;
        lock (connectionsByRemoteIdentifier)
        {
            connectionsByRemoteIdentifier.TryGetValue(remoteIdentifier, out connection);
        }

        return connection;
    }

    /// <summary>
    /// 轮询网络事件 - 新版服务端专用
    /// </summary>
    public void PollNetworkEvents()
    {
        server.PollEvents();
    }

    /// <summary>
    /// 获取当前连接数
    /// </summary>
    public int GetConnectedPeersCount()
    {
        return server.ConnectedPeersCount;
    }
}
