namespace NitroxModel.BelowZero.Enums
{
    /// <summary>
    /// Below Zero运动学状态 - 零度之下特有的物理状态
    /// </summary>
    public enum ZeroKinematicState
    {
        /// <summary>
        /// 静态状态
        /// </summary>
        Static,
        
        /// <summary>
        /// 动态状态
        /// </summary>
        Dynamic,
        
        /// <summary>
        /// 冻结状态 - Below Zero特有的冰冻机制
        /// </summary>
        Frozen,
        
        /// <summary>
        /// 漂浮状态
        /// </summary>
        Floating,
        
        /// <summary>
        /// 下沉状态
        /// </summary>
        Sinking
    }
}
