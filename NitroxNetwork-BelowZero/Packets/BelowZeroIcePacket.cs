using System;
using NitroxModel.BelowZero.Enums;
using NitroxNetwork.BelowZero.Core;

namespace NitroxNetwork.BelowZero.Packets
{
    /// <summary>
    /// Below Zero冰层管理数据包 - 零度之下特有的冰川系统
    /// </summary>
    public class BelowZeroIcePacket : BelowZeroNetworkPacket
    {
        public override string PacketType => "BelowZero.Ice";
        public override NetworkChannel Channel => NetworkChannel.Construction;

        /// <summary>
        /// 冰层ID
        /// </summary>
        public string IceId { get; set; } = string.Empty;

        /// <summary>
        /// 冰层操作类型
        /// </summary>
        public IceActionType ActionType { get; set; }

        /// <summary>
        /// 冰层位置
        /// </summary>
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }

        /// <summary>
        /// 冰层大小
        /// </summary>
        public float SizeX { get; set; }
        public float SizeY { get; set; }
        public float SizeZ { get; set; }

        /// <summary>
        /// 冰层厚度
        /// </summary>
        public float Thickness { get; set; }

        /// <summary>
        /// 冰层硬度（0.0-1.0）
        /// </summary>
        public float Hardness { get; set; }

        /// <summary>
        /// 融化进度（0.0-1.0）
        /// </summary>
        public float MeltProgress { get; set; }

        /// <summary>
        /// 是否可破坏
        /// </summary>
        public bool IsBreakable { get; set; }

        /// <summary>
        /// 是否正在融化
        /// </summary>
        public bool IsMelting { get; set; }

        /// <summary>
        /// 冰层类型
        /// </summary>
        public BelowZeroIceType IceType { get; set; }

        /// <summary>
        /// 破坏者ID（玩家或工具）
        /// </summary>
        public string BreakerId { get; set; } = string.Empty;

        /// <summary>
        /// 破坏工具类型
        /// </summary>
        public string BreakingTool { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{base.ToString()} Ice: {IceId}, Action: {ActionType}, Type: {IceType}";
        }
    }

    /// <summary>
    /// 冰层操作类型
    /// </summary>
    public enum IceActionType
    {
        /// <summary>
        /// 创建冰层
        /// </summary>
        Create,
        
        /// <summary>
        /// 破坏冰层
        /// </summary>
        Break,
        
        /// <summary>
        /// 开始融化
        /// </summary>
        StartMelting,
        
        /// <summary>
        /// 停止融化
        /// </summary>
        StopMelting,
        
        /// <summary>
        /// 更新融化进度
        /// </summary>
        UpdateMelting,
        
        /// <summary>
        /// 完全融化
        /// </summary>
        CompleteMelt,
        
        /// <summary>
        /// 重新冻结
        /// </summary>
        Refreeze,
        
        /// <summary>
        /// 状态同步
        /// </summary>
        Sync
    }

    /// <summary>
    /// Below Zero冰层类型
    /// </summary>
    public enum BelowZeroIceType
    {
        /// <summary>
        /// 普通冰层
        /// </summary>
        Regular,
        
        /// <summary>
        /// 厚冰层
        /// </summary>
        Thick,
        
        /// <summary>
        /// 可破坏冰层
        /// </summary>
        Breakable,
        
        /// <summary>
        /// 永久冰层
        /// </summary>
        Permanent,
        
        /// <summary>
        /// 地热冰层
        /// </summary>
        Thermal,
        
        /// <summary>
        /// 晶体冰层
        /// </summary>
        Crystal
    }
}
