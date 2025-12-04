using System;
using NitroxModel.Packets;
using NitroxServer.Communication.Packets.Processors.Abstract;
using NitroxServer.GameLogic;

namespace NitroxServer.Communication.Packets.Processors;

public class PingRequestProcessor : AuthenticatedPacketProcessor<PingRequest>
{
    public override void Process(PingRequest packet, Player player)
    {
        // 立即回复ping请求，包含原始时间戳和服务器时间戳
        long serverTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        PingResponse response = new(packet.Timestamp, serverTimestamp);
        
        player.SendPacket(response);
        
        // 记录ping请求（用于调试）
        Log.Debug($"[PING] 处理来自 {player.Name} 的ping请求 | 原始时间戳: {packet.Timestamp} | 服务器时间戳: {serverTimestamp}");
    }
}
