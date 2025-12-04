using System;
using System.Collections.Generic;
using System.Linq;
using NitroxClient.Communication.Abstract;
using NitroxModel.Core;
using NitroxModel.Packets;
using UnityEngine;

namespace NitroxClient.GameLogic.HUD;

/// <summary>
/// 管理客户端到服务器的延迟检测
/// </summary>
public class NetworkPingManager
{
    private readonly IPacketSender packetSender;
    private readonly Queue<long> pingHistory = new();
    private const int MAX_PING_HISTORY = 5; // 保留最近5次ping结果用于平均值计算
    
    private float lastPingTime = 0f;
    private const float PING_INTERVAL = 2f; // 每2秒发送一次ping
    
    public long CurrentPing { get; private set; } = -1;
    public long AveragePing { get; private set; } = -1;
    
    public event Action<long> OnPingUpdated;
    
    public NetworkPingManager(IPacketSender packetSender)
    {
        this.packetSender = packetSender;
        Log.Info("[PING] NetworkPingManager 已初始化");
    }
    
    public void Update()
    {
        // 定期发送ping请求
        if (Time.time - lastPingTime >= PING_INTERVAL)
        {
            SendPingRequest();
            lastPingTime = Time.time;
        }
    }
    
    private void SendPingRequest()
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        PingRequest pingRequest = new(timestamp);
        packetSender.Send(pingRequest);
        
        Log.Debug($"[PING] 发送ping请求 | 时间戳: {timestamp}");
    }
    
    public void UpdatePing(long roundTripTime)
    {
        CurrentPing = roundTripTime;
        
        // 添加到历史记录
        pingHistory.Enqueue(roundTripTime);
        if (pingHistory.Count > MAX_PING_HISTORY)
        {
            pingHistory.Dequeue();
        }
        
        // 计算平均延迟
        AveragePing = (long)pingHistory.Average();
        
        // 通知UI更新
        OnPingUpdated?.Invoke(AveragePing);
        
        Log.Debug($"[PING] 延迟更新 | 当前: {CurrentPing}ms | 平均: {AveragePing}ms");
    }
    
    public string GetPingDisplayText()
    {
        if (AveragePing == -1)
        {
            return "延迟: --ms";
        }

        string colorHex;
        if (AveragePing < 50)
        {
            colorHex = "#00FF00"; // Green (Excellent)
        }
        else if (AveragePing < 100)
        {
            colorHex = "#FFFF00"; // Yellow (Good)
        }
        else if (AveragePing < 200)
        {
            colorHex = "#FFA500"; // Orange (Average)
        }
        else
        {
            colorHex = "#FF0000"; // Red (Poor)
        }

        return $"延迟: <color={colorHex}>{AveragePing}ms</color>";
    }
}
