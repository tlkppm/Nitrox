namespace NitroxModel.BelowZero.Enums
{
    /// <summary>
    /// Below Zero处理器类型枚举
    /// </summary>
    public enum ProcessType
    {
        /// <summary>
        /// 服务器端处理
        /// </summary>
        ServerSide,
        
        /// <summary>
        /// 客户端处理
        /// </summary>
        ClientSide,
        
        /// <summary>
        /// 双端处理
        /// </summary>
        Both,
        
        /// <summary>
        /// 只处理主机
        /// </summary>
        HostOnly
    }
}
