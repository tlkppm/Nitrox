using System.Collections.Generic;
using NitroxModel.BelowZero.Enums;
using NitroxModel.Logger;

namespace NitroxModel.BelowZero.Features
{
    /// <summary>
    /// Below Zero网络管理器 - 直接套用Below Zero的网络架构
    /// </summary>
    public static class BelowZeroNetwork
    {
        /// <summary>
        /// 当前玩家是否为主机
        /// </summary>
        public static bool IsHost { get; set; }

        /// <summary>
        /// 是否处于多人游戏模式
        /// </summary>
        public static bool IsMultiplayerActive { get; set; }

        /// <summary>
        /// Below Zero特有的基地面片管理
        /// </summary>
        public static BelowZeroBaseFacePiece BaseFacePiece { get; private set; } = new();

        /// <summary>
        /// 动态实体管理
        /// </summary>
        public static BelowZeroDynamicEntity DynamicEntity { get; private set; } = new();

        /// <summary>
        /// 静态实体管理
        /// </summary>
        public static BelowZeroStaticEntity StaticEntity { get; private set; } = new();

        /// <summary>
        /// Below Zero身份标识符
        /// </summary>
        public static BelowZeroIdentifier Identifier { get; private set; } = new();

        /// <summary>
        /// 当前会话信息
        /// </summary>
        public static BelowZeroSession Session { get; private set; } = new();

        /// <summary>
        /// 获取网络通道总数
        /// </summary>
        public static int GetChannelCount()
        {
            return System.Enum.GetValues(typeof(NetworkChannel)).Length;
        }

        /// <summary>
        /// 初始化Below Zero网络系统
        /// </summary>
        public static void Initialize()
        {
            Log.Info("初始化Below Zero网络系统...");
            IsHost = false;
            IsMultiplayerActive = false;
            
            // 重置所有管理器
            BaseFacePiece = new BelowZeroBaseFacePiece();
            DynamicEntity = new BelowZeroDynamicEntity();
            StaticEntity = new BelowZeroStaticEntity();
            Identifier = new BelowZeroIdentifier();
            Session = new BelowZeroSession();
            
            Log.Info("Below Zero网络系统初始化完成");
        }

        /// <summary>
        /// 清理网络资源
        /// </summary>
        public static void Cleanup()
        {
            Log.Info("清理Below Zero网络资源...");
            IsHost = false;
            IsMultiplayerActive = false;
        }
    }

    /// <summary>
    /// Below Zero基地面片管理器
    /// </summary>
    public class BelowZeroBaseFacePiece
    {
        private readonly Dictionary<string, object> facePieces = new();

        public void AddFacePiece(string id, object piece)
        {
            facePieces[id] = piece;
        }

        public T GetFacePiece<T>(string id) where T : class
        {
            return facePieces.TryGetValue(id, out var piece) ? piece as T : null;
        }
    }

    /// <summary>
    /// Below Zero动态实体管理器
    /// </summary>
    public class BelowZeroDynamicEntity
    {
        private readonly Dictionary<string, object> entities = new();

        public void RegisterEntity(string id, object entity)
        {
            entities[id] = entity;
        }

        public T GetEntity<T>(string id) where T : class
        {
            return entities.TryGetValue(id, out var entity) ? entity as T : null;
        }
    }

    /// <summary>
    /// Below Zero静态实体管理器
    /// </summary>
    public class BelowZeroStaticEntity
    {
        private readonly Dictionary<string, object> staticEntities = new();

        public void RegisterStaticEntity(string id, object entity)
        {
            staticEntities[id] = entity;
        }

        public T GetStaticEntity<T>(string id) where T : class
        {
            return staticEntities.TryGetValue(id, out var entity) ? entity as T : null;
        }
    }

    /// <summary>
    /// Below Zero身份标识管理器
    /// </summary>
    public class BelowZeroIdentifier
    {
        private int currentId = 1000; // Below Zero起始ID

        public string GenerateId()
        {
            return $"bz_{++currentId}";
        }

        public bool IsValidBelowZeroId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.StartsWith("bz_");
        }
    }

    /// <summary>
    /// Below Zero会话管理器
    /// </summary>
    public class BelowZeroSession
    {
        public string SessionId { get; set; } = string.Empty;
        public Dictionary<string, object> SessionData { get; } = new();
        public bool IsActive { get; set; }

        public void StartSession(string sessionId)
        {
            SessionId = sessionId;
            IsActive = true;
            SessionData.Clear();
            Log.Info($"Below Zero会话已启动: {sessionId}");
        }

        public void EndSession()
        {
            IsActive = false;
            SessionData.Clear();
            Log.Info($"Below Zero会话已结束: {SessionId}");
            SessionId = string.Empty;
        }
    }
}
