using System;

namespace NitroxModel.Packets;

[Serializable]
public class PingRequest : Packet
{
    public long Timestamp { get; }
    
    public PingRequest(long timestamp) : base()
    {
        Timestamp = timestamp;
    }
}
