namespace NitroxModel.BelowZero.Enums
{
    /// <summary>
    /// Below Zero网络通道枚举 - 直接套用Below Zero的优秀设计
    /// </summary>
    public enum NetworkChannel : byte
    {   
        /// <summary>
        /// 默认通道
        /// </summary>
        Default,

        /// <summary>
        /// 建造通道
        /// </summary>
        Construction,

        /// <summary>
        /// 启动通道
        /// </summary>
        Startup,

        /// <summary>
        /// 世界加载启动通道
        /// </summary>
        StartupWorldLoaded,

        /// <summary>
        /// 能量传输通道
        /// </summary>
        EnergyTransmission,

        /// <summary>
        /// 玩家动画通道
        /// </summary>
        PlayerAnimation,

        /// <summary>
        /// 移动通道
        /// </summary>
        PlayerMovement,
        VehicleMovement,
        EntityMovement,
        FishMovement,
    }
}
