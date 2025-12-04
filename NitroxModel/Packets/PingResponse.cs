using System;

namespace NitroxModel.Packets;

[Serializable]
public class PingResponse : Packet
{
    public long OriginalTimestamp { get; }
    public long ServerTimestamp { get; }
    
    public PingResponse(long originalTimestamp, long serverTimestamp) : base()
    {
        OriginalTimestamp = originalTimestamp;
        ServerTimestamp = serverTimestamp;
    }
}
