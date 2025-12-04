using System;
using NitroxModel.BelowZero.Enums;
using NitroxNetwork.BelowZero.Core;

namespace NitroxNetwork.BelowZero.Packets
{
    /// <summary>
    /// Below Zero Seatruck数据包 - 海卡车专用网络包
    /// </summary>
    public class BelowZeroSeatruckPacket : BelowZeroNetworkPacket
    {
        public override string PacketType => "BelowZero.Seatruck";
        public override NetworkChannel Channel => NetworkChannel.VehicleMovement;

        /// <summary>
        /// Seatruck ID
        /// </summary>
        public string SeatruckId { get; set; } = string.Empty;

        /// <summary>
        /// 操作类型
        /// </summary>
        public SeatruckActionType ActionType { get; set; }

        /// <summary>
        /// 位置信息
        /// </summary>
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }

        /// <summary>
        /// 旋转信息
        /// </summary>
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public float RotationW { get; set; }

        /// <summary>
        /// 速度信息
        /// </summary>
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float VelocityZ { get; set; }

        /// <summary>
        /// Seatruck模块配置
        /// </summary>
        public string[] AttachedModules { get; set; } = new string[0];

        /// <summary>
        /// 驾驶员ID
        /// </summary>
        public string PilotId { get; set; } = string.Empty;

        /// <summary>
        /// 能量状态
        /// </summary>
        public float Energy { get; set; }

        /// <summary>
        /// 健康状态
        /// </summary>
        public float Health { get; set; }


    }

    /// <summary>
    /// Seatruck操作类型
    /// </summary>
    public enum SeatruckActionType
    {
        /// <summary>
        /// 移动更新
        /// </summary>
        MovementUpdate,
        
        /// <summary>
        /// 模块连接
        /// </summary>
        ModuleAttach,
        
        /// <summary>
        /// 模块分离
        /// </summary>
        ModuleDetach,
        
        /// <summary>
        /// 进入驾驶
        /// </summary>
        EnterPilot,
        
        /// <summary>
        /// 退出驾驶
        /// </summary>
        ExitPilot,
        
        /// <summary>
        /// 能量更新
        /// </summary>
        EnergyUpdate,
        
        /// <summary>
        /// 损坏更新
        /// </summary>
        DamageUpdate,
        
        /// <summary>
        /// 完全同步
        /// </summary>
        FullSync
    }
}
