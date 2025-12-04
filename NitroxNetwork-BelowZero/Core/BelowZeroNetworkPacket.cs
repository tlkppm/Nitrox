using System;
using LiteNetLib.Utils;
using NitroxModel.BelowZero.Enums;
using NitroxModel.Networking;
using NitroxModel.Packets;

namespace NitroxNetwork.BelowZero.Core
{
    /// <summary>
    /// Below Zero网络数据包基类 - 套用Below Zero的数据包设计
    /// </summary>
    public abstract class BelowZeroNetworkPacket : Packet
    {
        /// <summary>
        /// 数据包类型标识
        /// </summary>
        public abstract string PacketType { get; }

        /// <summary>
        /// 网络通道
        /// </summary>
        public virtual NetworkChannel Channel => NetworkChannel.Default;

        /// <summary>
        /// 数据包优先级
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// 数据包时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 发送者ID
        /// </summary>
        public string SenderId { get; set; } = string.Empty;

        protected BelowZeroNetworkPacket()
        {
            // 设置Below Zero特有的网络通道映射
            UdpChannel = MapChannelToUdpChannel(Channel);
        }

        /// <summary>
        /// 将Below Zero网络通道映射到Nitrox UDP通道
        /// </summary>
        private UdpChannelId MapChannelToUdpChannel(NetworkChannel channel)
        {
            return channel switch
            {
                NetworkChannel.PlayerMovement or 
                NetworkChannel.VehicleMovement or 
                NetworkChannel.EntityMovement or 
                NetworkChannel.FishMovement => UdpChannelId.MOVEMENTS,
                _ => UdpChannelId.DEFAULT
            };
        }

        /// <summary>
        /// 获取数据包大小估算
        /// </summary>
        public virtual int GetEstimatedSize()
        {
            return PacketType.Length + 8 + SenderId.Length; // 基础大小
        }

        public override string ToString()
        {
            return $"[{PacketType}] From: {SenderId} At: {Timestamp:HH:mm:ss.fff}";
        }
    }

    /// <summary>
    /// Below Zero服务器数据包基类
    /// </summary>
    public abstract class BelowZeroServerPacket : BelowZeroNetworkPacket
    {
        /// <summary>
        /// 服务器时间戳
        /// </summary>
        public DateTime ServerTimestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Below Zero客户端数据包基类
    /// </summary>
    public abstract class BelowZeroClientPacket : BelowZeroNetworkPacket
    {
        /// <summary>
        /// 客户端版本
        /// </summary>
        public string ClientVersion { get; set; } = "1.0.0";
    }
}
