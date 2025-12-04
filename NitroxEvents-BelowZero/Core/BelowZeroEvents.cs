using System;
using NitroxModel.BelowZero.Enums;

namespace NitroxEvents.BelowZero.Core
{
    /// <summary>
    /// Below Zero基础事件类
    /// </summary>
    public abstract class BelowZeroBaseEvent : IBelowZeroEvent
    {
        public virtual string EventId { get; } = Guid.NewGuid().ToString();
        public virtual DateTime Timestamp { get; } = DateTime.UtcNow;
        public virtual ProcessType ProcessType { get; } = ProcessType.Both;

        protected BelowZeroBaseEvent()
        {
        }

        protected BelowZeroBaseEvent(string eventId)
        {
            EventId = eventId;
        }
    }

    /// <summary>
    /// Seatruck相关事件
    /// </summary>
    public class SeatruckEvent : BelowZeroBaseEvent
    {
        public string SeatruckId { get; set; }
        public string ActionType { get; set; }
        public string PlayerId { get; set; }
        public object Data { get; set; }

        public SeatruckEvent(string seatruckId, string actionType, string playerId = "", object data = null)
        {
            SeatruckId = seatruckId;
            ActionType = actionType;
            PlayerId = playerId;
            Data = data;
        }
    }

    /// <summary>
    /// 天气变化事件
    /// </summary>
    public class WeatherChangeEvent : BelowZeroBaseEvent
    {
        public string WeatherType { get; set; }
        public float Intensity { get; set; }
        public float Temperature { get; set; }
        public string RegionId { get; set; }

        public WeatherChangeEvent(string weatherType, float intensity, float temperature, string regionId = "")
        {
            WeatherType = weatherType;
            Intensity = intensity;
            Temperature = temperature;
            RegionId = regionId;
        }
    }

    /// <summary>
    /// 冰层变化事件
    /// </summary>
    public class IceChangeEvent : BelowZeroBaseEvent
    {
        public string IceId { get; set; }
        public string ActionType { get; set; }
        public string PlayerId { get; set; }
        public object IceData { get; set; }

        public IceChangeEvent(string iceId, string actionType, string playerId = "", object iceData = null)
        {
            IceId = iceId;
            ActionType = actionType;
            PlayerId = playerId;
            IceData = iceData;
        }
    }

    /// <summary>
    /// 玩家温度变化事件
    /// </summary>
    public class PlayerTemperatureEvent : BelowZeroBaseEvent
    {
        public string PlayerId { get; set; }
        public float CurrentTemperature { get; set; }
        public float TargetTemperature { get; set; }
        public string LocationBiome { get; set; }
        public bool IsInShelter { get; set; }

        public PlayerTemperatureEvent(string playerId, float currentTemp, float targetTemp, string biome, bool inShelter)
        {
            PlayerId = playerId;
            CurrentTemperature = currentTemp;
            TargetTemperature = targetTemp;
            LocationBiome = biome;
            IsInShelter = inShelter;
        }
    }

    /// <summary>
    /// 载具模块变化事件
    /// </summary>
    public class VehicleModuleEvent : BelowZeroBaseEvent
    {
        public string VehicleId { get; set; }
        public string ModuleType { get; set; }
        public string ActionType { get; set; } // Attach, Detach, Upgrade
        public string PlayerId { get; set; }
        public object ModuleData { get; set; }

        public VehicleModuleEvent(string vehicleId, string moduleType, string actionType, string playerId, object moduleData = null)
        {
            VehicleId = vehicleId;
            ModuleType = moduleType;
            ActionType = actionType;
            PlayerId = playerId;
            ModuleData = moduleData;
        }
    }

    /// <summary>
    /// 地热能源事件
    /// </summary>
    public class ThermalEnergyEvent : BelowZeroBaseEvent
    {
        public string ThermalVentId { get; set; }
        public float EnergyOutput { get; set; }
        public float Temperature { get; set; }
        public bool IsActive { get; set; }
        public string ConnectedStructureId { get; set; }

        public ThermalEnergyEvent(string ventId, float energyOutput, float temperature, bool isActive, string structureId = "")
        {
            ThermalVentId = ventId;
            EnergyOutput = energyOutput;
            Temperature = temperature;
            IsActive = isActive;
            ConnectedStructureId = structureId;
        }
    }

    /// <summary>
    /// 生物群系探索事件
    /// </summary>
    public class BiomeExplorationEvent : BelowZeroBaseEvent
    {
        public string PlayerId { get; set; }
        public string BiomeId { get; set; }
        public string BiomeName { get; set; }
        public bool IsFirstVisit { get; set; }
        public float ExplorationProgress { get; set; }

        public BiomeExplorationEvent(string playerId, string biomeId, string biomeName, bool isFirst, float progress)
        {
            PlayerId = playerId;
            BiomeId = biomeId;
            BiomeName = biomeName;
            IsFirstVisit = isFirst;
            ExplorationProgress = progress;
        }
    }

    /// <summary>
    /// 建造事件 - Below Zero特有建筑
    /// </summary>
    public class BelowZeroConstructionEvent : BelowZeroBaseEvent
    {
        public string PlayerId { get; set; }
        public string StructureType { get; set; }
        public string ActionType { get; set; } // Build, Deconstruct, Upgrade
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public object StructureData { get; set; }

        public BelowZeroConstructionEvent(string playerId, string structureType, string actionType, 
            float x, float y, float z, object structureData = null)
        {
            PlayerId = playerId;
            StructureType = structureType;
            ActionType = actionType;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            StructureData = structureData;
        }
    }

    /// <summary>
    /// 网络连接事件
    /// </summary>
    public class BelowZeroNetworkEvent : BelowZeroBaseEvent
    {
        public string ConnectionId { get; set; }
        public string EventType { get; set; } // Connect, Disconnect, Sync, Error
        public string PlayerId { get; set; }
        public object EventData { get; set; }

        public BelowZeroNetworkEvent(string connectionId, string eventType, string playerId = "", object eventData = null)
        {
            ConnectionId = connectionId;
            EventType = eventType;
            PlayerId = playerId;
            EventData = eventData;
        }
    }

    /// <summary>
    /// 配方解锁事件 - Below Zero特有配方
    /// </summary>
    public class BelowZeroRecipeUnlockEvent : BelowZeroBaseEvent
    {
        public string PlayerId { get; set; }
        public string RecipeId { get; set; }
        public string RecipeName { get; set; }
        public string UnlockMethod { get; set; } // Scanner, Fragment, Story
        public bool IsNewRecipe { get; set; }

        public BelowZeroRecipeUnlockEvent(string playerId, string recipeId, string recipeName, string unlockMethod, bool isNew)
        {
            PlayerId = playerId;
            RecipeId = recipeId;
            RecipeName = recipeName;
            UnlockMethod = unlockMethod;
            IsNewRecipe = isNew;
        }
    }
}
