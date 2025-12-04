using System;
using System.Collections.Concurrent;
using NitroxModel.Logger;
using NitroxServer.BelowZero.Core;

namespace NitroxServer.BelowZero.Managers
{
    /// <summary>
    /// Below Zero玩家管理器
    /// </summary>
    public class BelowZeroPlayerManager
    {
        private bool isInitialized = false;
        private readonly ConcurrentDictionary<string, BelowZeroPlayer> players = new();

        public BelowZeroPlayerManager()
        {
            Log.Debug("Below Zero玩家管理器已创建");
        }

        /// <summary>
        /// 初始化玩家管理器
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            Log.Info("初始化Below Zero玩家管理器...");
            
            isInitialized = true;
            Log.Info("Below Zero玩家管理器初始化完成");
        }

        /// <summary>
        /// 更新玩家状态
        /// </summary>
        public void Update()
        {
            if (!isInitialized)
                return;

            try
            {
                // 更新所有玩家状态
                foreach (var player in players.Values)
                {
                    UpdatePlayerStatus(player);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"玩家管理器更新异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理玩家管理器
        /// </summary>
        public void Cleanup()
        {
            Log.Info("清理Below Zero玩家管理器...");
            
            players.Clear();
            isInitialized = false;
            
            Log.Info("Below Zero玩家管理器清理完成");
        }

        /// <summary>
        /// 添加玩家
        /// </summary>
        public bool AddPlayer(BelowZeroPlayer player)
        {
            return players.TryAdd(player.Id, player);
        }

        /// <summary>
        /// 移除玩家
        /// </summary>
        public bool RemovePlayer(string playerId)
        {
            return players.TryRemove(playerId, out _);
        }

        /// <summary>
        /// 获取玩家
        /// </summary>
        public BelowZeroPlayer GetPlayer(string playerId)
        {
            players.TryGetValue(playerId, out var player);
            return player;
        }

        /// <summary>
        /// 更新玩家状态
        /// </summary>
        private void UpdatePlayerStatus(BelowZeroPlayer player)
        {
            // 更新玩家位置、状态等
        }
    }
}
