using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.DataStructures.Util;
using NitroxModel.Packets;
using NitroxServer.Communication.Packets.Processors.Abstract;
using NitroxServer.GameLogic;
using NitroxServer.GameLogic.Entities;

namespace NitroxServer.Communication.Packets.Processors;

public class PickupItemPacketProcessor : AuthenticatedPacketProcessor<PickupItem>
{
    private readonly EntityRegistry entityRegistry;
    private readonly WorldEntityManager worldEntityManager;
    private readonly PlayerManager playerManager;
    private readonly SimulationOwnershipData simulationOwnershipData;

    public PickupItemPacketProcessor(EntityRegistry entityRegistry, WorldEntityManager worldEntityManager, PlayerManager playerManager, SimulationOwnershipData simulationOwnershipData)
    {
        this.entityRegistry = entityRegistry;
        this.worldEntityManager = worldEntityManager;
        this.playerManager = playerManager;
        this.simulationOwnershipData = simulationOwnershipData;
    }

    public override void Process(PickupItem packet, Player player)
    {
        NitroxId id = packet.Item.Id;
        string itemType = packet.Item.GetType().Name;
        string classId = "未知"; // 简化处理，避免接口依赖问题
        
        // 记录物品拾取事件
        Log.Info($"[物品事件] 玩家拾取物品 | 玩家: '{player.Name}' | 物品类型: {itemType} | ClassID: {classId} | 物品ID: {id}");
        
        if (simulationOwnershipData.RevokeOwnerOfId(id))
        {
            ushort serverId = ushort.MaxValue;
            SimulationOwnershipChange simulationOwnershipChange = new(id, serverId, SimulationLockType.TRANSIENT);
            playerManager.SendPacketToAllPlayers(simulationOwnershipChange);
            Log.Info($"[模拟权限] 撤销物品模拟权限 | 物品ID: {id} | 转移给服务端");
        }

        StopTrackingExistingWorldEntity(id);

        entityRegistry.AddOrUpdate(packet.Item);

        // Have other players respawn the item inside the inventory.
        playerManager.SendPacketToOtherPlayers(new SpawnEntities(packet.Item, forceRespawn: true), player);
        
        Log.Info($"[物品同步] 向其他玩家同步物品状态 | 物品ID: {id} | 同步给: {playerManager.GetConnectedPlayers().Count - 1} 个其他玩家");
    }

    private void StopTrackingExistingWorldEntity(NitroxId id)
    {
        Optional<Entity> entity = entityRegistry.GetEntityById(id);

        if (entity.HasValue && entity.Value is WorldEntity worldEntity)
        {
            // Do not track this entity in the open world anymore.
            worldEntityManager.StopTrackingEntity(worldEntity);
        }
    }
}
