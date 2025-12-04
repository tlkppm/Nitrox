using System.Collections.Generic;
using System.Linq;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Unity;
using NitroxServer.GameLogic.Entities.Spawning;

namespace NitroxServer.GameLogic.Entities.Spawning
{
    /// <summary>
    /// 服务器端生物生成管理器 - 解决多人游戏中鱼群过度生成的问题
    /// </summary>
    public class CreatureSpawnManager
    {
        // 记录每个区域已生成的生物数量
        private readonly Dictionary<AbsoluteEntityCell, Dictionary<string, int>> spawnedCreaturesByCell = new();
        
        // 每个区域的最大生物密度限制
        private readonly Dictionary<string, int> maxCreaturesPerBiome = new()
        {
            { "safe_shallows", 15 },      // 安全浅滩
            { "kelp_forest", 12 },        // 海带森林  
            { "grassy_plateaus", 10 },    // 草原高地
            { "mushroom_forest", 8 },     // 蘑菇森林
            { "bulb_zone", 6 },           // 球茎区
            { "grand_reef", 8 },          // 大珊瑚礁
            { "dunes", 5 },               // 沙丘
            { "mountains", 4 },           // 山脉
            { "underwater_islands", 7 },   // 水下岛屿
            { "sparse_reef", 6 },         // 稀疏珊瑚礁
            { "blood_kelp", 4 },          // 血海带
            { "deep_grand_reef", 5 },     // 深层大珊瑚礁
            { "lava_zone", 3 },           // 熔岩区
            { "lost_river", 2 },          // 失落之河
            { "default", 8 }              // 默认值
        };

        private readonly object lockObject = new object();

        /// <summary>
        /// 检查是否可以在指定位置生成生物
        /// </summary>
        public bool CanSpawnCreature(AbsoluteEntityCell cell, string classId, string biomeType)
        {
            lock (lockObject)
            {
                // 获取当前区域已生成的生物
                if (!spawnedCreaturesByCell.TryGetValue(cell, out var spawnedCreatures))
                {
                    spawnedCreatures = new Dictionary<string, int>();
                    spawnedCreaturesByCell[cell] = spawnedCreatures;
                }

                // 获取此类型生物的当前数量
                spawnedCreatures.TryGetValue(classId, out int currentCount);

                // 获取生物群落的最大密度限制
                if (!maxCreaturesPerBiome.TryGetValue(biomeType, out int maxCount))
                {
                    maxCount = maxCreaturesPerBiome["default"];
                }

                // 计算该区域的总生物数量
                int totalCreatures = spawnedCreatures.Values.Sum();

                // 应用多重限制检查
                bool canSpawn = currentCount < 3 &&           // 每种生物最多3个
                               totalCreatures < maxCount &&    // 总数不超过生物群落限制
                               ShouldSpawnBasedOnProbability(classId, currentCount); // 基于概率的生成控制

                // 记录生物生成检查结果
                if (!canSpawn)
                {
                    Log.Debug($"[生物生成] 生成限制 | 细胞: {cell} | 生物: {classId} | 当前数量: {currentCount}/3 | 区域总数: {totalCreatures}/{maxCount} | 生物群落: {biomeType}");
                }

                return canSpawn;
            }
        }

        /// <summary>
        /// 记录生物生成
        /// </summary>
        public void RegisterCreatureSpawn(AbsoluteEntityCell cell, string classId)
        {
            lock (lockObject)
            {
                if (!spawnedCreaturesByCell.TryGetValue(cell, out var spawnedCreatures))
                {
                    spawnedCreatures = new Dictionary<string, int>();
                    spawnedCreaturesByCell[cell] = spawnedCreatures;
                }

                spawnedCreatures.TryGetValue(classId, out int currentCount);
                spawnedCreatures[classId] = currentCount + 1;
                
                // 记录生物生成成功
                int totalCreatures = spawnedCreatures.Values.Sum();
                Log.Info($"[生物生成] 生物已生成 | 细胞: {cell} | 生物: {classId} | 数量: {currentCount + 1} | 区域总数: {totalCreatures}");
            }
        }

        /// <summary>
        /// 记录生物死亡/移除
        /// </summary>
        public void UnregisterCreature(AbsoluteEntityCell cell, string classId)
        {
            lock (lockObject)
            {
                if (spawnedCreaturesByCell.TryGetValue(cell, out var spawnedCreatures))
                {
                    if (spawnedCreatures.TryGetValue(classId, out int currentCount) && currentCount > 0)
                    {
                        spawnedCreatures[classId] = currentCount - 1;
                        
                        // 如果数量为0，移除条目以节省内存
                        if (spawnedCreatures[classId] == 0)
                        {
                            spawnedCreatures.Remove(classId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 基于当前数量的概率生成控制
        /// </summary>
        private bool ShouldSpawnBasedOnProbability(string classId, int currentCount)
        {
            // 随着同类生物数量增加，生成概率降低
            float probability = currentCount switch
            {
                0 => 1.0f,      // 100% 概率生成第一个
                1 => 0.6f,      // 60% 概率生成第二个
                2 => 0.2f,      // 20% 概率生成第三个
                _ => 0.0f       // 不再生成更多
            };

            return new System.Random().NextDouble() < probability;
        }

        /// <summary>
        /// 获取区域生物统计信息（用于调试）
        /// </summary>
        public Dictionary<string, int> GetCellStatistics(AbsoluteEntityCell cell)
        {
            lock (lockObject)
            {
                if (spawnedCreaturesByCell.TryGetValue(cell, out var spawnedCreatures))
                {
                    return new Dictionary<string, int>(spawnedCreatures);
                }
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// 清理空的区域记录以节省内存
        /// </summary>
        public void CleanupEmptyCells()
        {
            lock (lockObject)
            {
                var emptyCells = spawnedCreaturesByCell
                    .Where(kvp => kvp.Value.Count == 0 || kvp.Value.Values.All(count => count == 0))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var cell in emptyCells)
                {
                    spawnedCreaturesByCell.Remove(cell);
                }
            }
        }
    }
}
