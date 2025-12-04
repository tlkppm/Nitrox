using System.Net;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using NitroxModel.Constants;

namespace NitroxServer.Communication;

public static class LANBroadcastServer
{
    private static NetManager server;
    private static EventBasedNetListener listener;
    private static Timer pollTimer;

    public static void Start(CancellationToken ct)
    {
        listener = new EventBasedNetListener();
        listener.NetworkReceiveUnconnectedEvent += NetworkReceiveUnconnected;
        server = new NetManager(listener);
        server.AutoRecycle = true;
        server.BroadcastReceiveEnabled = true;
        server.UnconnectedMessagesEnabled = true;

        bool started = false;
        foreach (int port in LANDiscoveryConstants.BROADCAST_PORTS)
        {
            if (server.Start(port))
            {
                Log.Info($"[LAN广播] 成功启动在端口 {port}");
                started = true;
                break;
            }
        }

        if (!started)
        {
            Log.Error("[LAN广播] 无法启动 - 所有端口都被占用");
            return;
        }

        pollTimer = new Timer(_ => server.PollEvents());
        pollTimer.Change(0, 100);
        Log.Info($"[LAN广播] LAN服务器发现功能已启动");
    }

    public static void Stop()
    {
        listener?.ClearNetworkReceiveUnconnectedEvent();
        server?.Stop();
        pollTimer?.Dispose();
        Log.Debug($"{nameof(LANBroadcastServer)} stopped");
    }

    private static void NetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType == UnconnectedMessageType.Broadcast)
        {
            string requestString = reader.GetString();
            Log.Info($"[LAN广播] 收到来自 {remoteEndPoint} 的广播请求: {requestString}");
            
            if (requestString == LANDiscoveryConstants.BROADCAST_REQUEST_STRING)
            {
                NetDataWriter writer = new();
                writer.Put(LANDiscoveryConstants.BROADCAST_RESPONSE_STRING);
                writer.Put(Server.Instance.Port);

                server.SendBroadcast(writer, remoteEndPoint.Port);
                Log.Info($"[LAN广播] 向 {remoteEndPoint} 发送响应 | 服务器端口: {Server.Instance.Port}");
            }
            else
            {
                Log.Warn($"[LAN广播] 收到无效的广播请求: {requestString} 来自 {remoteEndPoint}");
            }
        }
    }
}
