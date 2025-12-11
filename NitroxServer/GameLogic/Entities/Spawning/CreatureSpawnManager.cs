using System.Collections.Generic;
using System.Linq;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Unity;
using NitroxModel.Serialization;
using NitroxServer.GameLogic.Entities.Spawning;

namespace NitroxServer.GameLogic.Entities.Spawning
{
    public class CreatureSpawnManager
    {
        private readonly Dictionary<AbsoluteEntityCell, Dictionary<string, int>> spawnedCreaturesByCell = new();
        
        private readonly Dictionary<string, int> maxCreaturesPerBiome = new()
        {
            { "safe_shallows", 15 },
            { "kelp_forest", 12 },
            { "grassy_plateaus", 10 },
            { "mushroom_forest", 8 },
            { "bulb_zone", 6 },
            { "grand_reef", 8 },
            { "dunes", 5 },
            { "mountains", 4 },
            { "underwater_islands", 7 },
            { "sparse_reef", 6 },
            { "blood_kelp", 4 },
            { "deep_grand_reef", 5 },
            { "lava_zone", 3 },
            { "lost_river", 2 },
            { "default", 8 }
        };

        private readonly object lockObject = new object();

        public bool IsEnabled { get; set; } = true;
        public int MaxPerSpecies { get; set; } = 3;
        public int MaxPerCell { get; set; } = 15;

        public bool CanSpawnCreature(AbsoluteEntityCell cell, string classId, string biomeType)
        {
            if (!IsEnabled)
            {
                return true;
            }

            lock (lockObject)
            {
                if (!spawnedCreaturesByCell.TryGetValue(cell, out var spawnedCreatures))
                {
                    spawnedCreatures = new Dictionary<string, int>();
                    spawnedCreaturesByCell[cell] = spawnedCreatures;
                }

                spawnedCreatures.TryGetValue(classId, out int currentCount);

                if (!maxCreaturesPerBiome.TryGetValue(biomeType, out int biomeMax))
                {
                    biomeMax = maxCreaturesPerBiome["default"];
                }

                int effectiveMaxPerCell = System.Math.Min(MaxPerCell, biomeMax);
                int totalCreatures = spawnedCreatures.Values.Sum();

                bool canSpawn = currentCount < MaxPerSpecies &&
                               totalCreatures < effectiveMaxPerCell &&
                               ShouldSpawnBasedOnProbability(classId, currentCount);

                if (!canSpawn)
                {
                    Log.Debug($"[CreatureSpawn] Limit reached | Cell: {cell} | Species: {classId} | Count: {currentCount}/{MaxPerSpecies} | Total: {totalCreatures}/{effectiveMaxPerCell}");
                }

                return canSpawn;
            }
        }

        public void RegisterCreatureSpawn(AbsoluteEntityCell cell, string classId)
        {
            if (!IsEnabled) return;

            lock (lockObject)
            {
                if (!spawnedCreaturesByCell.TryGetValue(cell, out var spawnedCreatures))
                {
                    spawnedCreatures = new Dictionary<string, int>();
                    spawnedCreaturesByCell[cell] = spawnedCreatures;
                }

                spawnedCreatures.TryGetValue(classId, out int currentCount);
                spawnedCreatures[classId] = currentCount + 1;
            }
        }

        public void UnregisterCreature(AbsoluteEntityCell cell, string classId)
        {
            if (!IsEnabled) return;

            lock (lockObject)
            {
                if (spawnedCreaturesByCell.TryGetValue(cell, out var spawnedCreatures))
                {
                    if (spawnedCreatures.TryGetValue(classId, out int currentCount) && currentCount > 0)
                    {
                        spawnedCreatures[classId] = currentCount - 1;
                        if (spawnedCreatures[classId] == 0)
                        {
                            spawnedCreatures.Remove(classId);
                        }
                    }
                }
            }
        }

        private bool ShouldSpawnBasedOnProbability(string classId, int currentCount)
        {
            float probability = currentCount switch
            {
                0 => 1.0f,
                1 => 0.6f,
                2 => 0.2f,
                _ => 0.0f
            };
            return new System.Random().NextDouble() < probability;
        }

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
