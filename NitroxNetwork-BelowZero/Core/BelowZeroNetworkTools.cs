using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using NitroxModel.BelowZero.Enums;
using NitroxModel.Logger;
using NitroxModel.Networking;

namespace NitroxNetwork.BelowZero.Core
{
    /// <summary>
    /// Below Zero网络工具类 - 直接套用Below Zero的网络管理逻辑
    /// </summary>
    public static class BelowZeroNetworkTools
    {
        private static readonly ConcurrentDictionary<string, Type> PacketTypeRegistry = new();
        private static readonly ConcurrentDictionary<NetworkChannel, int> ChannelStatistics = new();

        /// <summary>
        /// 注册数据包类型
        /// </summary>
        public static void RegisterPacketType<T>() where T : BelowZeroNetworkPacket, new()
        {
            var packet = new T();
            PacketTypeRegistry[packet.PacketType] = typeof(T);
            Log.Debug($"注册Below Zero数据包类型: {packet.PacketType}");
        }

        /// <summary>
        /// 获取数据包类型
        /// </summary>
        public static Type GetPacketType(string packetType)
        {
            return PacketTypeRegistry.TryGetValue(packetType, out var type) ? type : null;
        }

        /// <summary>
        /// 创建数据包实例
        /// </summary>
        public static T CreatePacket<T>() where T : BelowZeroNetworkPacket, new()
        {
            return new T();
        }

        /// <summary>
        /// 序列化数据包
        /// </summary>
        public static byte[] SerializePacket(BelowZeroNetworkPacket packet)
        {
            try
            {
                RecordChannelUsage(packet.Channel);
                return packet.Serialize(); // 使用Nitrox的序列化方法
            }
            catch (Exception ex)
            {
                Log.Error($"序列化Below Zero数据包失败: {packet.PacketType}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 反序列化数据包
        /// </summary>
        public static BelowZeroNetworkPacket DeserializePacket(byte[] data)
        {
            try
            {
                return NitroxModel.Packets.Packet.Deserialize(data) as BelowZeroNetworkPacket;
            }
            catch (Exception ex)
            {
                Log.Error($"反序列化Below Zero数据包失败, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 转换Nitrox传输方式到LiteNetLib
        /// </summary>
        public static DeliveryMethod ToLiteNetLibDeliveryMethod(NitroxDeliveryMethod.DeliveryMethod nitroxMethod)
        {
            return nitroxMethod switch
            {
                NitroxDeliveryMethod.DeliveryMethod.UNRELIABLE_SEQUENCED => DeliveryMethod.Sequenced,
                NitroxDeliveryMethod.DeliveryMethod.RELIABLE_UNORDERED => DeliveryMethod.ReliableUnordered,
                NitroxDeliveryMethod.DeliveryMethod.RELIABLE_ORDERED => DeliveryMethod.ReliableOrdered,
                NitroxDeliveryMethod.DeliveryMethod.RELIABLE_ORDERED_LAST => DeliveryMethod.ReliableSequenced,
                _ => DeliveryMethod.ReliableOrdered
            };
        }

        /// <summary>
        /// 获取网络通道总数
        /// </summary>
        public static int GetChannelCount()
        {
            return Enum.GetValues(typeof(NetworkChannel)).Length;
        }

        /// <summary>
        /// 记录通道使用情况
        /// </summary>
        private static void RecordChannelUsage(NetworkChannel channel)
        {
            ChannelStatistics.AddOrUpdate(channel, 1, (key, oldValue) => oldValue + 1);
        }

        /// <summary>
        /// 获取通道统计信息
        /// </summary>
        public static Dictionary<NetworkChannel, int> GetChannelStatistics()
        {
            return ChannelStatistics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public static void ResetStatistics()
        {
            ChannelStatistics.Clear();
            Log.Debug("Below Zero网络统计信息已重置");
        }

        /// <summary>
        /// 验证数据包完整性
        /// </summary>
        public static bool ValidatePacket(BelowZeroNetworkPacket packet)
        {
            if (packet == null)
            {
                Log.Warn("数据包为空");
                return false;
            }

            if (string.IsNullOrEmpty(packet.PacketType))
            {
                Log.Warn("数据包类型为空");
                return false;
            }

            if (packet.Timestamp == default)
            {
                Log.Warn("数据包时间戳无效");
                return false;
            }

            // Below Zero特有的验证：检查时间戳是否过期（5分钟）
            if (DateTime.UtcNow - packet.Timestamp > TimeSpan.FromMinutes(5))
            {
                Log.Warn($"数据包已过期: {packet.PacketType}, 时间差: {DateTime.UtcNow - packet.Timestamp}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 计算网络延迟
        /// </summary>
        public static TimeSpan CalculateLatency(DateTime sentTime)
        {
            return DateTime.UtcNow - sentTime;
        }

        /// <summary>
        /// 压缩数据包（简单实现）
        /// </summary>
        public static byte[] CompressPacket(byte[] data)
        {
            // 这里可以实现真正的压缩算法，现在简单返回原数据
            // Below Zero项目中有专门的压缩器实现
            return data;
        }

        /// <summary>
        /// 解压缩数据包
        /// </summary>
        public static byte[] DecompressPacket(byte[] compressedData)
        {
            // 对应的解压缩实现
            return compressedData;
        }

        /// <summary>
        /// 获取网络诊断信息
        /// </summary>
        public static string GetNetworkDiagnostics()
        {
            var stats = GetChannelStatistics();
            var totalPackets = stats.Values.Sum();
            
            var diagnostics = $"Below Zero网络诊断信息:\n";
            diagnostics += $"已注册数据包类型: {PacketTypeRegistry.Count}\n";
            diagnostics += $"总数据包数量: {totalPackets}\n";
            diagnostics += "通道使用情况:\n";
            
            foreach (var kvp in stats.OrderByDescending(kvp => kvp.Value))
            {
                var channel = kvp.Key;
                var count = kvp.Value;
                var percentage = totalPackets > 0 ? (count * 100.0 / totalPackets) : 0;
                diagnostics += $"  {channel}: {count} ({percentage:F1}%)\n";
            }
            
            return diagnostics;
        }
    }
}
