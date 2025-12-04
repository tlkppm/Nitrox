using System.Collections.Generic;
using System.Linq;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.Packets;
using NitroxServer.Communication.Packets.Processors.Abstract;
using NitroxServer.GameLogic.Entities;

namespace NitroxServer.Communication.Packets.Processors;

public class CellVisibilityChangedProcessor : AuthenticatedPacketProcessor<CellVisibilityChanged>
{
    private readonly EntitySimulation entitySimulation;
    private readonly WorldEntityManager worldEntityManager;

    public CellVisibilityChangedProcessor(EntitySimulation entitySimulation, WorldEntityManager worldEntityManager)
    {
        this.entitySimulation = entitySimulation;
        this.worldEntityManager = worldEntityManager;
    }

    public override void Process(CellVisibilityChanged packet, Player player)
    {
        // 记录细胞可见性变化事件
        if (packet.Added.Length > 0 || packet.Removed.Length > 0)
        {
            Log.Info($"[细胞同步] 玩家 '{player.Name}' 的视野变化 | 新增细胞: {packet.Added.Length} | 移除细胞: {packet.Removed.Length}");
        }
        
        player.AddCells(packet.Added);
        player.RemoveCells(packet.Removed);

        List<Entity> totalEntities = [];
        List<SimulatedEntity> totalSimulationChanges = [];

        foreach (AbsoluteEntityCell addedCell in packet.Added)
        {
            worldEntityManager.LoadUnspawnedEntities(addedCell.BatchId, false);

            List<SimulatedEntity> cellSimulationChanges = entitySimulation.GetSimulationChangesForCell(player, addedCell);
            List<WorldEntity> cellWorldEntities = worldEntityManager.GetEntities(addedCell);
            
            totalSimulationChanges.AddRange(cellSimulationChanges);
            totalEntities.AddRange(cellWorldEntities.Cast<Entity>());
            
            // 记录细胞内容加载详情
            if (cellWorldEntities.Count > 0 || cellSimulationChanges.Count > 0)
            {
                Log.Info($"[细胞加载] 细胞 {addedCell} | 实体数量: {cellWorldEntities.Count} | 模拟变化: {cellSimulationChanges.Count}");
            }
        }

        foreach (AbsoluteEntityCell removedCell in packet.Removed)
        {
            entitySimulation.FillWithRemovedCells(player, removedCell, totalSimulationChanges);
            Log.Debug($"[细胞卸载] 玩家 '{player.Name}' 离开细胞: {removedCell}");
        }

        // Simulation update must be broadcasted before the entities are spawned
        if (totalSimulationChanges.Count > 0)
        {
            entitySimulation.BroadcastSimulationChanges(new(totalSimulationChanges));
            Log.Info($"[模拟同步] 广播模拟变化 | 变化数量: {totalSimulationChanges.Count} | 目标玩家: '{player.Name}'");
        }

        // We send this data whether or not it's empty because the client needs to know about it (see Terrain)
        player.SendPacket(new SpawnEntities(totalEntities, packet.Added, true));
    }
}
