using System.Collections.Generic;
using NitroxModel.BelowZero.Enums;
using NitroxModel.Logger;

namespace NitroxModel.BelowZero.Features
{
    /// <summary>
    /// Below Zero世界管理器 - 套用Below Zero的世界特性
    /// </summary>
    public static class BelowZeroWorld
    {
        /// <summary>
        /// Below Zero特有的生物群系管理
        /// </summary>
        public static BelowZeroBiomeManager BiomeManager { get; private set; } = new();

        /// <summary>
        /// 天气系统管理器
        /// </summary>
        public static BelowZeroWeatherManager WeatherManager { get; private set; } = new();

        /// <summary>
        /// 冰川系统管理器 - Below Zero独有
        /// </summary>
        public static BelowZeroIceManager IceManager { get; private set; } = new();

        /// <summary>
        /// 地热系统管理器
        /// </summary>
        public static BelowZeroThermalManager ThermalManager { get; private set; } = new();

        /// <summary>
        /// Below Zero载具管理器
        /// </summary>
        public static BelowZeroVehicleManager VehicleManager { get; private set; } = new();

        /// <summary>
        /// 初始化Below Zero世界系统
        /// </summary>
        public static void Initialize()
        {
            Log.Info("初始化Below Zero世界系统...");
            
            BiomeManager = new BelowZeroBiomeManager();
            WeatherManager = new BelowZeroWeatherManager();
            IceManager = new BelowZeroIceManager();
            ThermalManager = new BelowZeroThermalManager();
            VehicleManager = new BelowZeroVehicleManager();
            
            Log.Info("Below Zero世界系统初始化完成");
        }

        /// <summary>
        /// 清理世界资源
        /// </summary>
        public static void Cleanup()
        {
            Log.Info("清理Below Zero世界资源...");
            BiomeManager.Clear();
            WeatherManager.Reset();
            IceManager.Reset();
            ThermalManager.Reset();
            VehicleManager.Clear();
        }
    }

    /// <summary>
    /// Below Zero生物群系管理器
    /// </summary>
    public class BelowZeroBiomeManager
    {
        private readonly Dictionary<string, BelowZeroBiome> biomes = new();

        public void RegisterBiome(string biomeId, BelowZeroBiome biome)
        {
            biomes[biomeId] = biome;
            Log.Debug($"注册Below Zero生物群系: {biomeId}");
        }

        public BelowZeroBiome GetBiome(string biomeId)
        {
            return biomes.TryGetValue(biomeId, out var biome) ? biome : null;
        }

        public void Clear()
        {
            biomes.Clear();
        }
    }

    /// <summary>
    /// Below Zero生物群系定义
    /// </summary>
    public class BelowZeroBiome
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Temperature { get; set; } // Below Zero的温度系统
        public bool IsIcyBiome { get; set; } // 是否为冰冷生物群系
        public List<string> UniqueCreatures { get; set; } = new();
        public List<string> UniqueResources { get; set; } = new();
    }

    /// <summary>
    /// Below Zero天气管理器
    /// </summary>
    public class BelowZeroWeatherManager
    {
        public string CurrentWeather { get; private set; } = "Clear";
        public float WindStrength { get; private set; } = 0f;
        public float Temperature { get; private set; } = -10f; // Below Zero的低温环境
        public bool IsBlizzard { get; private set; } = false;

        public void SetWeather(string weather, float windStrength = 0f, float temperature = -10f)
        {
            CurrentWeather = weather;
            WindStrength = windStrength;
            Temperature = temperature;
            IsBlizzard = weather == "Blizzard";
            
            Log.Debug($"Below Zero天气更新: {weather}, 温度: {temperature}°C");
        }

        public void Reset()
        {
            CurrentWeather = "Clear";
            WindStrength = 0f;
            Temperature = -10f;
            IsBlizzard = false;
        }
    }

    /// <summary>
    /// Below Zero冰川管理器
    /// </summary>
    public class BelowZeroIceManager
    {
        private readonly Dictionary<string, IceFormation> iceFormations = new();

        public void RegisterIceFormation(string id, IceFormation formation)
        {
            iceFormations[id] = formation;
        }

        public IceFormation GetIceFormation(string id)
        {
            return iceFormations.TryGetValue(id, out var formation) ? formation : null;
        }

        public void Reset()
        {
            iceFormations.Clear();
        }
    }

    /// <summary>
    /// 冰层结构定义
    /// </summary>
    public class IceFormation
    {
        public string Id { get; set; }
        public float Thickness { get; set; }
        public bool IsBreakable { get; set; }
        public float MeltingPoint { get; set; }
    }

    /// <summary>
    /// Below Zero地热管理器
    /// </summary>
    public class BelowZeroThermalManager
    {
        private readonly Dictionary<string, ThermalVent> thermalVents = new();

        public void RegisterThermalVent(string id, ThermalVent vent)
        {
            thermalVents[id] = vent;
        }

        public ThermalVent GetThermalVent(string id)
        {
            return thermalVents.TryGetValue(id, out var vent) ? vent : null;
        }

        public void Reset()
        {
            thermalVents.Clear();
        }
    }

    /// <summary>
    /// 地热喷口定义
    /// </summary>
    public class ThermalVent
    {
        public string Id { get; set; }
        public float Temperature { get; set; }
        public float EnergyOutput { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Below Zero载具管理器
    /// </summary>
    public class BelowZeroVehicleManager
    {
        private readonly Dictionary<string, BelowZeroVehicle> vehicles = new();

        public void RegisterVehicle(string id, BelowZeroVehicle vehicle)
        {
            vehicles[id] = vehicle;
            Log.Debug($"注册Below Zero载具: {id} ({vehicle.Type})");
        }

        public BelowZeroVehicle GetVehicle(string id)
        {
            return vehicles.TryGetValue(id, out var vehicle) ? vehicle : null;
        }

        public void Clear()
        {
            vehicles.Clear();
        }
    }

    /// <summary>
    /// Below Zero载具定义
    /// </summary>
    public class BelowZeroVehicle
    {
        public string Id { get; set; }
        public string Type { get; set; } // SeaTruck, Snowfox, Prawn等
        public float Health { get; set; }
        public float Energy { get; set; }
        public Dictionary<string, object> Modules { get; set; } = new();
        public bool IsDeployed { get; set; }
    }
}
