using System;
using System.Collections.Generic;
using NitroxModel.Packets;
using NitroxServer.Communication.Packets.Processors.Abstract;
using NitroxServer.GameLogic;

namespace NitroxServer.Communication.Packets.Processors;

public class DefaultServerPacketProcessor : AuthenticatedPacketProcessor<Packet>
{
    private readonly PlayerManager playerManager;

    private readonly HashSet<Type> loggingPacketBlackList = new()
    {
        typeof(AnimationChangeEvent),
        typeof(PlayerMovement),
        typeof(ItemPosition),
        typeof(PlayerStats),
        typeof(StoryGoalExecuted),
        typeof(FMODAssetPacket),
        typeof(FMODCustomEmitterPacket),
        typeof(FMODCustomLoopingEmitterPacket),
        typeof(FMODStudioEmitterPacket),
        typeof(PlayerCinematicControllerCall),
        typeof(TorpedoShot),
        typeof(TorpedoHit),
        typeof(TorpedoTargetAcquired),
        typeof(StasisSphereShot),
        typeof(StasisSphereHit),
        typeof(SeaTreaderChunkPickedUp),
        typeof(ToggleLights)
    };

    /// <summary>
    /// Packet types which don't have a server packet processor but should not be transmitted
    /// </summary>
    private readonly HashSet<Type> defaultPacketProcessorBlacklist = new()
    {
        typeof(GameModeChanged), typeof(DropSimulationOwnership),
    };

    public DefaultServerPacketProcessor(PlayerManager playerManager)
    {
        this.playerManager = playerManager;
    }

    public override void Process(Packet packet, Player player)
    {
        string packetType = packet.GetType().Name;
        
        if (!loggingPacketBlackList.Contains(packet.GetType()))
        {
            Log.Debug($"Using default packet processor for: {packet} and player {player.Id}");
        }
        
        // 特别记录我们关心的世界事件包类型是否被默认处理器处理
        if (packetType == "EntitySpawnedByClient" || packetType == "CellVisibilityChanged" || packetType == "PickupItem")
        {
            Log.Warn($"[默认处理器] 世界事件包 {packetType} 被默认处理器处理！这意味着没有找到专用处理器 | 玩家: {player.Name}");
        }

        if (defaultPacketProcessorBlacklist.Contains(packet.GetType()))
        {
            Log.ErrorOnce($"Player {player.Name} [{player.Id}] sent a packet which is blacklisted by the server. It's likely that the said player is using a modified version of Nitrox and action could be taken accordingly.");
            return;
        }
        playerManager.SendPacketToOtherPlayers(packet, player);
    }
}
