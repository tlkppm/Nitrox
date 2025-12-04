using System;
using NitroxModel.BelowZero.Enums;
using NitroxNetwork.BelowZero.Core;

namespace NitroxNetwork.BelowZero.Packets
{
    /// <summary>
    /// Below Zero天气同步数据包 - Below Zero特有的天气系统
    /// </summary>
    public class BelowZeroWeatherPacket : BelowZeroServerPacket
    {
        public override string PacketType => "BelowZero.Weather";
        public override NetworkChannel Channel => NetworkChannel.Default;

        /// <summary>
        /// 天气类型
        /// </summary>
        public BelowZeroWeatherType WeatherType { get; set; }

        /// <summary>
        /// 天气强度（0.0 - 1.0）
        /// </summary>
        public float Intensity { get; set; }

        /// <summary>
        /// 环境温度（摄氏度）
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// 风力强度
        /// </summary>
        public float WindStrength { get; set; }

        /// <summary>
        /// 风向（角度）
        /// </summary>
        public float WindDirection { get; set; }

        /// <summary>
        /// 可见度（米）
        /// </summary>
        public float Visibility { get; set; }

        /// <summary>
        /// 是否有极光
        /// </summary>
        public bool HasAurora { get; set; }

        /// <summary>
        /// 极光强度
        /// </summary>
        public float AuroraIntensity { get; set; }

        /// <summary>
        /// 天气持续时间（秒）
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// 天气变化区域ID
        /// </summary>
        public string RegionId { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{base.ToString()} Weather: {WeatherType}, Temp: {Temperature}°C, Wind: {WindStrength}";
        }
    }

    /// <summary>
    /// Below Zero天气类型
    /// </summary>
    public enum BelowZeroWeatherType
    {
        /// <summary>
        /// 晴朗
        /// </summary>
        Clear,
        
        /// <summary>
        /// 多云
        /// </summary>
        Cloudy,
        
        /// <summary>
        /// 小雪
        /// </summary>
        LightSnow,
        
        /// <summary>
        /// 大雪
        /// </summary>
        HeavySnow,
        
        /// <summary>
        /// 暴雪
        /// </summary>
        Blizzard,
        
        /// <summary>
        /// 冰雨
        /// </summary>
        FreezingRain,
        
        /// <summary>
        /// 雾天
        /// </summary>
        Fog,
        
        /// <summary>
        /// 强风
        /// </summary>
        HighWind,
        
        /// <summary>
        /// 极光天气
        /// </summary>
        Aurora,
        
        /// <summary>
        /// 极地风暴
        /// </summary>
        PolarStorm
    }
}
