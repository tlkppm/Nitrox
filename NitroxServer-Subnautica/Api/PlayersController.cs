using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NitroxModel.Core;
using NitroxServer.GameLogic;

namespace Nitrox.Server.Subnautica.Api;

/// <summary>
/// 提供玩家信息的 Web API 控制器
/// 用于启动器查询当前在线玩家列表
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    /// <summary>
    /// 获取当前在线玩家列表
    /// GET /api/players
    /// </summary>
    /// <returns>玩家列表 JSON</returns>
    [HttpGet]
    public IActionResult GetPlayers()
    {
        try
        {
            // 从 DI 容器获取 PlayerManager
            var playerManager = NitroxServiceLocator.LocateService<PlayerManager>();
            if (playerManager == null)
            {
                return StatusCode(500, new { error = "PlayerManager 不可用" });
            }

            var connectedPlayers = playerManager.GetConnectedPlayers();
            
            var playerList = connectedPlayers.Select(player => new
            {
                id = player.Id,
                name = player.Name,
                permissions = player.Permissions.ToString(),
                gameMode = player.GameMode.ToString(),
                // 计算在线时长（使用 Connection 建立时间）
                connectionTime = DateTime.Now // 简化版本，实际应该从连接建立时间计算
            }).ToList();

            return Ok(new
            {
                success = true,
                count = playerList.Count,
                players = playerList,
                serverTime = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 获取服务器状态信息
    /// GET /api/players/status
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetServerStatus()
    {
        try
        {
            var server = NitroxServiceLocator.LocateService<NitroxServer.Server>();
            var playerManager = NitroxServiceLocator.LocateService<PlayerManager>();
            
            if (server == null || playerManager == null)
            {
                return StatusCode(500, new { error = "服务未初始化" });
            }

            var connectedCount = playerManager.GetConnectedPlayers().Count;
            
            return Ok(new
            {
                success = true,
                serverName = server.Name,
                port = server.Port,
                isRunning = server.IsRunning,
                playerCount = connectedCount,
                serverTime = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}

