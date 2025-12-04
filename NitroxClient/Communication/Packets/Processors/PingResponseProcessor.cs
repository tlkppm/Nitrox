using System;
using NitroxClient.Communication.Packets.Processors.Abstract;
using NitroxClient.GameLogic.HUD;
using NitroxModel.Core;
using NitroxModel.Packets;

namespace NitroxClient.Communication.Packets.Processors;

public class PingResponseProcessor : ClientPacketProcessor<PingResponse>
{
    private readonly NetworkPingManager pingManager;
    
    public PingResponseProcessor(NetworkPingManager pingManager)
    {
        this.pingManager = pingManager;
    }

    public override void Process(PingResponse packet)
    {
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long roundTripTime = currentTime - packet.OriginalTimestamp;
        
        // 更新延迟管理器
        pingManager.UpdatePing(roundTripTime);
        
        Log.Debug($"[PING] 收到ping响应 | 往返时间: {roundTripTime}ms | 服务器时间差: {packet.ServerTimestamp - packet.OriginalTimestamp}ms");
    }
}
