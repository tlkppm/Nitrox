using NitroxClient.GameLogic.HUD;
using NitroxModel.Core;
using UnityEngine;

namespace NitroxClient.MonoBehaviours;

/// <summary>
/// 定期更新延迟管理器的MonoBehaviour组件
/// </summary>
public class PingManagerUpdater : MonoBehaviour
{
    private NetworkPingManager pingManager;
    private bool isInitialized = false;
    
    private void Start()
    {
        // 延迟初始化，等待依赖注入完成
        InvokeRepeating(nameof(TryInitialize), 1f, 1f);
    }
    
    private void TryInitialize()
    {
        if (!isInitialized)
        {
            try
            {
                pingManager = NitroxServiceLocator.LocateService<NetworkPingManager>();
                if (pingManager != null)
                {
                    isInitialized = true;
                    CancelInvoke(nameof(TryInitialize));
                    Log.Info("[PING] PingManagerUpdater 初始化完成");
                }
            }
            catch (System.Exception ex)
            {
                Log.Debug($"[PING] PingManager 尚未准备就绪: {ex.Message}");
            }
        }
    }
    
    private void Update()
    {
        if (isInitialized && pingManager != null)
        {
            pingManager.Update();
        }
    }
    
    private void OnDestroy()
    {
        CancelInvoke();
    }
}
