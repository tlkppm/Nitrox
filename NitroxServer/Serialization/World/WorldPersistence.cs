using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NitroxModel.Core;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.GameLogic.Entities;
using NitroxModel.DataStructures.Util;
using NitroxModel.Networking;
using NitroxModel.Platforms.OS.Shared;
using NitroxModel.Serialization;
using NitroxModel.Server;
using NitroxServer.GameLogic;
using NitroxServer.GameLogic.Bases;
using NitroxServer.GameLogic.Entities;
using NitroxServer.GameLogic.Entities.Spawning;
using NitroxServer.GameLogic.Players;
using NitroxServer.GameLogic.Unlockables;
using NitroxServer.Helper;
using NitroxServer.Resources;
using NitroxServer.Serialization.Upgrade;

namespace NitroxServer.Serialization.World;

public class WorldPersistence
{
    public const string BACKUP_DATE_TIME_FORMAT = "yyyy-MM-dd HH.mm.ss";
    public IServerSerializer Serializer { get; private set; }
    private string FileEnding => Serializer?.FileEnding ?? "";

    private readonly ServerProtoBufSerializer protoBufSerializer;
    private readonly ServerJsonSerializer jsonSerializer;
    private readonly SubnauticaServerConfig config;
    private readonly RandomStartGenerator randomStart;
    private readonly IWorldModifier worldModifier;
    private readonly SaveDataUpgrade[] upgrades;
    private readonly RandomSpawnSpoofer randomSpawnSpoofer;
    private readonly NtpSyncer ntpSyncer;

    public WorldPersistence(
        ServerProtoBufSerializer protoBufSerializer,
        ServerJsonSerializer jsonSerializer,
        SubnauticaServerConfig config,
        RandomStartGenerator randomStart,
        IWorldModifier worldModifier,
        SaveDataUpgrade[] upgrades,
        RandomSpawnSpoofer randomSpawnSpoofer,
        NtpSyncer ntpSyncer
    )
    {
        this.protoBufSerializer = protoBufSerializer;
        this.jsonSerializer = jsonSerializer;
        this.config = config;
        this.randomStart = randomStart;
        this.worldModifier = worldModifier;
        this.upgrades = upgrades;
        this.randomSpawnSpoofer = randomSpawnSpoofer;
        this.ntpSyncer = ntpSyncer;

        UpdateSerializer(config.SerializerMode);
    }

    public bool Save(World world, string saveDir) => Save(PersistedWorldData.From(world), saveDir);

    public void BackUp(string saveDir)
    {
        if (config.MaxBackups < 1)
        {
            Log.Info($"No backup was made (\"{nameof(config.MaxBackups)}\" is equal to 0)");
            return;
        }
        string backupDir = Path.Combine(saveDir, "Backups");
        string tempOutDir = Path.Combine(backupDir, $"Backup - {DateTime.Now.ToString(BACKUP_DATE_TIME_FORMAT)}");
        Directory.CreateDirectory(backupDir);

        try
        {
            // Prepare backup location
            Directory.CreateDirectory(tempOutDir);
            string newZipFile = $"{tempOutDir}.zip";
            if (File.Exists(newZipFile))
            {
                File.Delete(newZipFile);
            }
            foreach (string file in Directory.GetFiles(saveDir))
            {
                File.Copy(file, Path.Combine(tempOutDir, Path.GetFileName(file)));
            }

            FileSystem.Instance.ZipFilesInDirectory(tempOutDir, newZipFile);
            Directory.Delete(tempOutDir, true);
            Log.Info("World backed up");

            // Prune old backups
            FileInfo[] backups = Directory.EnumerateFiles(backupDir)
                                          .Select(f => new FileInfo(f))
                                          .Where(f => f is { Extension: ".zip" } info && info.Name.Contains("Backup - "))
                                          .OrderBy(f => File.GetCreationTime(f.FullName))
                                          .ToArray();
            if (backups.Length > config.MaxBackups)
            {
                int numBackupsToDelete = backups.Length - Math.Max(1, config.MaxBackups);
                for (int i = 0; i < numBackupsToDelete; i++)
                {
                    backups[i].Delete();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while backing up world");
            if (Directory.Exists(tempOutDir))
            {
                Directory.Delete(tempOutDir, true); // Delete the outZip folder that is sometimes left
            }
        }
    }

    public World Load(string saveName)
    {
        Console.WriteLine("[DEBUG] WorldPersistence.Load 开始执行");
        Console.WriteLine($"[DEBUG] 保存名称: {saveName}");
        
        string saveDir = Path.Combine(KeyValueStore.Instance.GetSavesFolderDir(), saveName);
        Console.WriteLine($"[DEBUG] 保存目录: {saveDir}");
        
        Optional<World> fileLoadedWorld = LoadFromFile(saveDir);
        if (fileLoadedWorld.HasValue)
        {
            Console.WriteLine("[DEBUG] 从文件加载世界成功");
            return fileLoadedWorld.Value;
        }

        Console.WriteLine("[DEBUG] 文件不存在，创建新世界");
        return CreateFreshWorld();
    }

    public World CreateWorld(PersistedWorldData pWorldData, NitroxGameMode gameMode)
    {
        Console.WriteLine("[DEBUG] CreateWorld 开始执行");
        
        string seed = pWorldData.WorldData.Seed;
        if (string.IsNullOrWhiteSpace(seed))
        {
#if DEBUG
            seed = "TCCBIBZXAB";
#else
            seed = StringHelper.GenerateRandomString(10);
#endif
        }
        Console.WriteLine($"[DEBUG] 世界种子: {seed}");
        
        // Initialized only once, just like UnityEngine.Random
        XORRandom.InitSeed(seed.GetHashCode());

        Log.Info($"Loading world with seed {seed}");

        Console.WriteLine("[DEBUG] 开始获取 EntityRegistry 服务");
        EntityRegistry entityRegistry = NitroxServiceLocator.LocateService<EntityRegistry>();
        Console.WriteLine("[DEBUG] EntityRegistry 服务获取完成");
        
        Console.WriteLine($"[DEBUG] 开始添加实体到注册表 ({pWorldData.EntityData.Entities.Count} 个实体)");
        entityRegistry.AddEntities(pWorldData.EntityData.Entities);
        Console.WriteLine("[DEBUG] 实体添加完成");
        
        Console.WriteLine("[DEBUG] 开始添加全局根实体");
        entityRegistry.AddEntitiesIgnoringDuplicate(pWorldData.GlobalRootData.Entities.OfType<Entity>().ToList());
        Console.WriteLine("[DEBUG] 全局根实体添加完成");

        Console.WriteLine("[DEBUG] 开始创建 World 对象");
        World world = new()
        {
            SimulationOwnershipData = new SimulationOwnershipData(),
            PlayerManager = new PlayerManager(pWorldData.PlayerData.GetPlayers(), config),
            EscapePodManager = new EscapePodManager(entityRegistry, randomStart, seed),
            EntityRegistry = entityRegistry,
            GameData = pWorldData.WorldData.GameData,
            GameMode = gameMode,
            Seed = seed,
            SessionSettings = new()
        };
        Console.WriteLine("[DEBUG] World 基础对象创建完成");

        Console.WriteLine("[DEBUG] 开始创建 TimeKeeper");
        world.TimeKeeper = new(world.PlayerManager, ntpSyncer, pWorldData.WorldData.GameData.StoryTiming.ElapsedSeconds, pWorldData.WorldData.GameData.StoryTiming.RealTimeElapsed);
        Console.WriteLine("[DEBUG] TimeKeeper 创建完成");
        
        Console.WriteLine("[DEBUG] 开始创建 StoryManager");
        world.StoryManager = new StoryManager(world.PlayerManager, pWorldData.WorldData.GameData.PDAState, pWorldData.WorldData.GameData.StoryGoals, world.TimeKeeper, seed, pWorldData.WorldData.GameData.StoryTiming.AuroraCountdownTime,
                                              pWorldData.WorldData.GameData.StoryTiming.AuroraWarningTime, pWorldData.WorldData.GameData.StoryTiming.AuroraRealExplosionTime);
        Console.WriteLine("[DEBUG] StoryManager 创建完成");
        
        Console.WriteLine("[DEBUG] 开始创建 ScheduleKeeper");
        world.ScheduleKeeper = new ScheduleKeeper(pWorldData.WorldData.GameData.PDAState, pWorldData.WorldData.GameData.StoryGoals, world.TimeKeeper, world.PlayerManager);
        Console.WriteLine("[DEBUG] ScheduleKeeper 创建完成");

        Console.WriteLine("[DEBUG] 开始创建 CreatureSpawnManager");
        // 创建生物生成管理器 - 用于控制鱼群过度生成
        world.CreatureSpawnManager = new CreatureSpawnManager();
        Console.WriteLine("[DEBUG] CreatureSpawnManager 创建完成");

        Console.WriteLine("[DEBUG] 开始创建 BatchEntitySpawner - 需要获取多个服务");
        world.BatchEntitySpawner = new BatchEntitySpawner(
            NitroxServiceLocator.LocateService<EntitySpawnPointFactory>(),
            NitroxServiceLocator.LocateService<IUweWorldEntityFactory>(),
            NitroxServiceLocator.LocateService<IUwePrefabFactory>(),
            pWorldData.WorldData.ParsedBatchCells,
            protoBufSerializer,
            NitroxServiceLocator.LocateService<IEntityBootstrapperManager>(),
            NitroxServiceLocator.LocateService<Dictionary<string, PrefabPlaceholdersGroupAsset>>(),
            pWorldData.WorldData.GameData.PDAState,
            randomSpawnSpoofer,
            world.CreatureSpawnManager,
            world.Seed
        );
        Console.WriteLine("[DEBUG] BatchEntitySpawner 创建完成");

        Console.WriteLine("[DEBUG] 开始创建 WorldEntityManager");
        world.WorldEntityManager = new WorldEntityManager(world.EntityRegistry, world.BatchEntitySpawner, world.PlayerManager);
        Console.WriteLine("[DEBUG] WorldEntityManager 创建完成");

        Console.WriteLine("[DEBUG] 开始创建 BuildingManager");
        world.BuildingManager = new BuildingManager(world.EntityRegistry, world.WorldEntityManager, config);
        Console.WriteLine("[DEBUG] BuildingManager 创建完成");

        Console.WriteLine("[DEBUG] 开始创建 EntitySimulation");
        ISimulationWhitelist simulationWhitelist = NitroxServiceLocator.LocateService<ISimulationWhitelist>();
        world.EntitySimulation = new EntitySimulation(world.EntityRegistry, world.WorldEntityManager, world.SimulationOwnershipData, world.PlayerManager, simulationWhitelist);
        Console.WriteLine("[DEBUG] EntitySimulation 创建完成");

        Console.WriteLine("[DEBUG] CreateWorld 执行完成");
        return world;
    }

    internal void UpdateSerializer(IServerSerializer serverSerializer)
    {
        Validate.NotNull(serverSerializer, "Serializer cannot be null");
        Serializer = serverSerializer;
    }

    internal void UpdateSerializer(ServerSerializerMode mode) => Serializer = mode == ServerSerializerMode.PROTOBUF ? protoBufSerializer : jsonSerializer;

    internal bool Save(PersistedWorldData persistedData, string saveDir)
    {
        try
        {
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }

            Serializer.Serialize(Path.Combine(saveDir, $"Version{FileEnding}"), new SaveFileVersion());
            Serializer.Serialize(Path.Combine(saveDir, $"PlayerData{FileEnding}"), persistedData.PlayerData);
            Serializer.Serialize(Path.Combine(saveDir, $"WorldData{FileEnding}"), persistedData.WorldData);
            Serializer.Serialize(Path.Combine(saveDir, $"GlobalRootData{FileEnding}"), persistedData.GlobalRootData);
            Serializer.Serialize(Path.Combine(saveDir, $"EntityData{FileEnding}"), persistedData.EntityData);

            using (config.Update(saveDir))
            {
                config.Seed = persistedData.WorldData.Seed;
            }

            Log.Info("World state saved");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save world :");
            return false;
        }
    }

    internal Optional<World> LoadFromFile(string saveDir)
    {
        Console.WriteLine("[DEBUG] LoadFromFile 开始执行");
        Console.WriteLine($"[DEBUG] 检查保存目录: {saveDir}");
        
        if (!Directory.Exists(saveDir) || !File.Exists(Path.Combine(saveDir, $"Version{FileEnding}")))
        {
            Console.WriteLine("[DEBUG] 保存目录不存在或版本文件不存在");
            Log.Warn("No previous save file found, creating a new one");
            return Optional.Empty;
        }

        Console.WriteLine("[DEBUG] 开始升级保存文件");
        UpgradeSave(saveDir);
        Console.WriteLine("[DEBUG] 保存文件升级完成");

        Console.WriteLine("[DEBUG] 开始从路径加载数据");
        PersistedWorldData persistedData = LoadDataFromPath(saveDir);
        Console.WriteLine("[DEBUG] 数据加载完成");

        if (persistedData == null)
        {
            Console.WriteLine("[DEBUG] 加载的数据为空");
            Log.Warn("No previous save file found, creating a new one");
            return Optional.Empty;
        }

        Console.WriteLine("[DEBUG] 开始创建世界对象");
        World world = CreateWorld(persistedData, config.GameMode);
        Console.WriteLine("[DEBUG] 世界对象创建完成");

        return Optional.Of(world);
    }

    internal PersistedWorldData LoadDataFromPath(string saveDir)
    {
        try
        {
            Console.WriteLine("[DEBUG] LoadDataFromPath 开始反序列化文件");
            
            Console.WriteLine("[DEBUG] 开始反序列化 PlayerData");
            var playerData = Serializer.Deserialize<PlayerData>(Path.Combine(saveDir, $"PlayerData{FileEnding}"));
            Console.WriteLine("[DEBUG] PlayerData 反序列化完成");
            
            Console.WriteLine("[DEBUG] 开始反序列化 WorldData (这可能需要较长时间)");
            var worldData = Serializer.Deserialize<WorldData>(Path.Combine(saveDir, $"WorldData{FileEnding}"));
            Console.WriteLine("[DEBUG] WorldData 反序列化完成");
            
            Console.WriteLine("[DEBUG] 开始反序列化 GlobalRootData");
            var globalRootData = Serializer.Deserialize<GlobalRootData>(Path.Combine(saveDir, $"GlobalRootData{FileEnding}"));
            Console.WriteLine("[DEBUG] GlobalRootData 反序列化完成");
            
            Console.WriteLine("[DEBUG] 开始反序列化 EntityData (这可能需要较长时间)");
            var entityData = Serializer.Deserialize<EntityData>(Path.Combine(saveDir, $"EntityData{FileEnding}"));
            Console.WriteLine("[DEBUG] EntityData 反序列化完成");
            
            PersistedWorldData persistedData = new()
            {
                PlayerData = playerData,
                WorldData = worldData,
                GlobalRootData = globalRootData,
                EntityData = entityData
            };

            Console.WriteLine("[DEBUG] 开始验证数据有效性");
            if (!persistedData.IsValid())
            {
                throw new InvalidDataException("Save files are not valid");
            }
            Console.WriteLine("[DEBUG] 数据验证通过");

            return persistedData;
        }
        catch (Exception ex)
        {
            // Check if the world was newly created using the world manager
            if (new FileInfo(Path.Combine(saveDir, $"Version{FileEnding}")).Length > 0)
            {
                // Give error saying that world could not be used, and to restore a backup
                Log.Error($"Could not load world, please restore one of your backups to continue using this world. : {ex.GetType()} {ex.Message}");

                throw;
            }
        }

        return null;
    }

    private World CreateFreshWorld()
    {
        Console.WriteLine("[DEBUG] CreateFreshWorld 开始执行");
        Log.Info(" 正在创建全新世界...");
        
        Console.WriteLine("[DEBUG] 创建空的世界数据结构");
        PersistedWorldData pWorldData = new()
        {
            EntityData = EntityData.From(new List<Entity>()),
            PlayerData = PlayerData.From(new List<Player>()),
            WorldData = new WorldData
            {
                GameData = new GameData
                {
                    PDAState = new PDAStateData(),
                    StoryGoals = new StoryGoalData(),
                    StoryTiming = new StoryTimingData()
                },
                ParsedBatchCells = new List<NitroxInt3>(),
                Seed = config.Seed
            },
            GlobalRootData = new GlobalRootData()
        };
        Console.WriteLine("[DEBUG] 世界数据结构创建完成");

        Console.WriteLine("[DEBUG] 开始使用空数据创建世界");
        World newWorld = CreateWorld(pWorldData, config.GameMode);
        Console.WriteLine("[DEBUG] 空世界创建完成");
        
        Console.WriteLine("[DEBUG] 开始应用世界修改器");
        worldModifier.ModifyWorld(newWorld);
        Console.WriteLine("[DEBUG] 世界修改器应用完成");

        Console.WriteLine("[DEBUG] CreateFreshWorld 执行完成");
        Log.Info("新世界创建完成！");
        return newWorld;
    }

    private void UpgradeSave(string saveDir)
    {
        SaveFileVersion saveFileVersion;

        try
        {
            saveFileVersion = Serializer.Deserialize<SaveFileVersion>(Path.Combine(saveDir, $"Version{FileEnding}"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error while upgrading save file. \"Version{FileEnding}\" couldn't be read.");
            return;
        }

        if (saveFileVersion == null || saveFileVersion.Version == NitroxEnvironment.Version)
        {
            return;
        }

        if (config.SerializerMode == ServerSerializerMode.PROTOBUF)
        {
            Log.Info("Can't upgrade while using ProtoBuf as serializer");
        }
        else
        {
            try
            {
                foreach (SaveDataUpgrade upgrade in upgrades)
                {
                    if (upgrade.TargetVersion > saveFileVersion.Version)
                    {
                        upgrade.UpgradeSaveFiles(saveDir, FileEnding);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while upgrading save file.");
                return;
            }

            Serializer.Serialize(Path.Combine(saveDir, $"Version{FileEnding}"), new SaveFileVersion());
            Log.Info($"Save file was upgraded to {NitroxEnvironment.Version}");
        }
    }
}
