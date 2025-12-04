using System;
using System.Collections.Generic;
using NitroxModel.Core;
using NitroxModel.Packets;
using NitroxModel.Packets.Processors.Abstract;
using NitroxServer.Communication.Packets.Processors;
using NitroxServer.Communication.Packets.Processors.Abstract;
using NitroxServer.GameLogic;

namespace NitroxServer.Communication.Packets
{
    public class PacketHandler
    {
        private readonly PlayerManager playerManager;
        private readonly DefaultServerPacketProcessor defaultServerPacketProcessor;
        private readonly Dictionary<Type, PacketProcessor> packetProcessorAuthCache = new();
        private readonly Dictionary<Type, PacketProcessor> packetProcessorUnauthCache = new();

        public PacketHandler(PlayerManager playerManager, DefaultServerPacketProcessor packetProcessor)
        {
            this.playerManager = playerManager;
            defaultServerPacketProcessor = packetProcessor;
        }

        public void Process(Packet packet, INitroxConnection connection)
        {
            string packetType = packet.GetType().Name;
            Player player = playerManager.GetPlayer(connection);
            
            if (player == null)
            {
                Log.Debug($"[包处理] 处理未认证数据包: {packetType}");
                ProcessUnauthenticated(packet, connection);
            }
            else
            {
                Log.Debug($"[包处理] 处理已认证数据包: {packetType} | 玩家: {player.Name}");
                ProcessAuthenticated(packet, player);
            }
        }

        private void ProcessAuthenticated(Packet packet, Player player)
        {
            Type packetType = packet.GetType();
            string typeName = packetType.Name;
            
            if (!packetProcessorAuthCache.TryGetValue(packetType, out PacketProcessor processor))
            {
                Type packetProcessorType = typeof(AuthenticatedPacketProcessor<>).MakeGenericType(packetType);
                var serviceOptional = NitroxServiceLocator.LocateOptionalService(packetProcessorType);
                processor = serviceOptional.HasValue ? serviceOptional.Value as PacketProcessor : null;
                packetProcessorAuthCache[packetType] = processor;
                
                Log.Debug($"[包处理器缓存] {typeName} | 处理器类型: {packetProcessorType.Name} | 找到处理器: {processor != null}");
            }

            if (processor != null)
            {
                // 特别记录我们关心的包类型
                if (typeName == "EntitySpawnedByClient" || typeName == "CellVisibilityChanged" || typeName == "PickupItem")
                {
                    Log.Info($"[世界事件] 正在处理 {typeName} 包 | 处理器: {processor.GetType().Name} | 玩家: {player.Name}");
                }
                
                try
                {
                    processor.ProcessPacket(packet, player);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error in packet processor {processor.GetType()}");
                }
            }
            else
            {
                Log.Debug($"[包处理器] 未找到 {typeName} 的专用处理器，使用默认处理器");
                defaultServerPacketProcessor.ProcessPacket(packet, player);
            }
        }

        private void ProcessUnauthenticated(Packet packet, INitroxConnection connection)
        {
            Type packetType = packet.GetType();
            if (!packetProcessorUnauthCache.TryGetValue(packetType, out PacketProcessor processor))
            {
                Type packetProcessorType = typeof(UnauthenticatedPacketProcessor<>).MakeGenericType(packetType);
                packetProcessorUnauthCache[packetType] = processor = NitroxServiceLocator.LocateOptionalService(packetProcessorType).Value as PacketProcessor;
            }
            if (processor == null)
            {
                Log.Warn($"Received invalid, unauthenticated packet: {packet}");
                return;
            }

            try
            {
                processor.ProcessPacket(packet, connection);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error in packet processor {processor.GetType()}");
            }
        }
    }
}
