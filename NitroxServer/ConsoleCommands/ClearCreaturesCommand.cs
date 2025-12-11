using System.Collections.Generic;
using System.Linq;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.Packets;
using NitroxServer.ConsoleCommands.Abstract;
using NitroxServer.ConsoleCommands.Abstract.Type;
using NitroxServer.GameLogic;
using NitroxServer.GameLogic.Entities;

namespace NitroxServer.ConsoleCommands
{
    internal class ClearCreaturesCommand : Command
    {
        private readonly EntityRegistry entityRegistry;
        private readonly WorldEntityManager worldEntityManager;
        private readonly PlayerManager playerManager;

        private static readonly HashSet<string> creatureTechTypes = new(System.StringComparer.OrdinalIgnoreCase)
        {
            "Peeper", "Bladderfish", "Boomerang", "Eyeye", "Garryfish", "Holefish", "Hoverfish", "LavaEyeye",
            "Oculus", "Reginald", "Spadefish", "Stalker", "BoneShark", "CaveCrawler", "Crash", "Floater",
            "Gasopod", "LavaLizard", "Mesmer", "RabbitRay", "Sandshark", "Shuttlebug", "Spinefish", "Warper",
            "Biter", "Bleeder", "Crabsnake", "CrabSquid", "GhostLeviathan", "GhostLeviathanJuvenile",
            "ReaperLeviathan", "SeaDragon", "SeaEmperor", "SeaTreader", "Shocker", "SpineEel",
            "Hoopfish", "ArcticPeeper", "Rockgrub", "Penguin", "PenguinBaby", "Pinnacarid",
            "SymbioteSmall", "Skyray", "LavaLarva", "Jellyray", "GhostRay", "Cutefish",
            "Jumper", "PrecursorDroid", "Rockpuncher", "SeaMonkey", "SnowStalker", "SnowStalkerBaby",
            "Chelicerate", "ShadowLeviathan", "VoidLeviathan", "IceWorm", "Squidshark", "Cryptosuchus",
            "GlowWhale", "Triops", "TitanHolefish", "FeatherFish", "FeatherFishRed", "Discus",
            "NootFish", "SpinnerFish", "TrivalveBlue", "TrivalveYellow", "ArcticRay", "ArrowRay",
            "BrinewingSchool", "LilyPaddler", "RockPuncher", "BruteShark"
        };

        public ClearCreaturesCommand(EntityRegistry entityRegistry, WorldEntityManager worldEntityManager, PlayerManager playerManager) 
            : base("clearcreatures", Perms.ADMIN, "清除指定物种的生物实体")
        {
            AddParameter(new TypeString("species", false, "要清除的物种名称（留空清除所有生物）"));
            this.entityRegistry = entityRegistry;
            this.worldEntityManager = worldEntityManager;
            this.playerManager = playerManager;
        }

        protected override void Execute(CallArgs args)
        {
            string speciesFilter = args.GetTillEnd(0)?.Trim();
            bool hasFilter = !string.IsNullOrEmpty(speciesFilter);

            List<Entity> allEntities = entityRegistry.GetAllEntities();
            List<WorldEntity> creaturesToRemove = new();

            foreach (Entity entity in allEntities)
            {
                if (entity is not WorldEntity worldEntity)
                    continue;

                string techTypeName = worldEntity.TechType?.Name ?? string.Empty;
                
                if (!IsCreature(techTypeName))
                    continue;

                if (hasFilter && !techTypeName.Contains(speciesFilter, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                creaturesToRemove.Add(worldEntity);
            }

            int removedCount = 0;
            foreach (WorldEntity creature in creaturesToRemove)
            {
                if (worldEntityManager.TryDestroyEntity(creature.Id, out _))
                {
                    playerManager.SendPacketToAllPlayers(new EntityDestroyed(creature.Id));
                    removedCount++;
                }
            }

            string message = hasFilter 
                ? $"已清除 {removedCount} 个 {speciesFilter} 生物实体"
                : $"已清除 {removedCount} 个生物实体";
            
            SendMessage(args.Sender, message);
            Log.Info($"[ClearCreatures] {message}");
        }

        private static bool IsCreature(string techTypeName)
        {
            if (string.IsNullOrEmpty(techTypeName))
                return false;

            return creatureTechTypes.Contains(techTypeName) ||
                   techTypeName.EndsWith("School") ||
                   techTypeName.Contains("Fish") ||
                   techTypeName.Contains("Creature");
        }
    }
}
