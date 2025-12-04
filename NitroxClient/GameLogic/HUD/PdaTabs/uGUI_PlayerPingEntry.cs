using System.Collections;
using System.Collections.Generic;
using NitroxClient.Communication.Abstract;
using NitroxClient.Communication.Packets.Processors;
using NitroxClient.GameLogic.HUD.Components;
using NitroxClient.GameLogic.PlayerLogic.PlayerModel.Abstract;
using NitroxClient.MonoBehaviours.Gui.Modals;
using NitroxModel_Subnautica.DataStructures;
using NitroxModel.Core;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.Packets;
using UnityEngine;
using UnityEngine.UI;
using UWE;
using TMPro;

namespace NitroxClient.GameLogic.HUD.PdaTabs;

public class uGUI_PlayerPingEntry : uGUI_PingEntry
{
    private uGUI_PlayerListTab parent;
    private INitroxPlayer player;
    private NetworkPingManager pingManager;
    private TextMeshProUGUI pingLabel; // 延迟显示标签

    public string PlayerName => player?.PlayerName ?? string.Empty;
    public bool IsLocalPlayer => player is LocalPlayer;
    private bool showPing;

    private bool muted
    {
        get
        {
            if (player is RemotePlayer remotePlayer && remotePlayer.PlayerContext != null)
            {
                return remotePlayer.PlayerContext.IsMuted;
            }
            // By default we don't care about the local state
            return false;
        }
    }

    public GameObject ShowObject;
    public GameObject MuteObject;
    public GameObject KickObject;
    public GameObject TeleportToObject;
    public GameObject TeleportToMeObject;

    public Sprite MutedSprite;
    public Sprite UnmutedSprite;
    public Sprite KickSprite;
    public Sprite TeleportToSprite;
    public Sprite TeleportToMeSprite;

    public new void Awake()
    {
        NitroxServiceLocator.LocateService<MutePlayerProcessor>().OnPlayerMuted += (playerId, _) =>
        {
            if (player is RemotePlayer remotePlayer && remotePlayer.PlayerId == playerId)
            {
                RefreshMuteButton();
            }
        };
        NitroxServiceLocator.LocateService<PermsChangedProcessor>().OnPermissionsChanged += (perms) => RefreshButtonsVisibility();
    }

    public IEnumerator Start()
    {
        // We must one frame so that the UI elements are initialized properly
        yield return null;
        // This action must happen after the yield so that they're correctly placed
        UpdateButtonsPosition();
        // We trigger it at least once so that the localizations are updated with the PlayerName
        OnLanguageChanged();
    }

    public void Initialize(string id, string name, uGUI_PlayerListTab parent)
    {
        this.id = id;
        this.parent = parent;
        this.pingManager = NitroxServiceLocator.LocateService<NetworkPingManager>();

        gameObject.SetActive(true);
        visibilityIcon.sprite = spriteVisible;
        // 新版游戏兼容性修复 - 直接使用Sprite
        try
        {
            var tabSprite = SpriteManager.Get(SpriteManager.Group.Tab, "TabInventory");
            icon.SetForegroundSprite(tabSprite);
        }
        catch (System.ArgumentException)
        {
            // 如果类型转换失败，使用默认图标或跳过
            Log.Warn("Failed to set tab inventory sprite for player ping entry");
        }
        showPing = true;

        UpdateLabel(name);
        SetupPingDisplay(); // 设置延迟显示
        OnLanguageChanged();

        CoroutineHost.StartCoroutine(AssignSprites());
    }

    public void OnLanguageChanged()
    {
        GetTooltip(ShowObject).TooltipText = GetLocalizedText(showPing ? "Nitrox_HidePing" : "Nitrox_ShowPing");
        GetTooltip(MuteObject).TooltipText = GetLocalizedText(muted ? "Nitrox_Unmute" : "Nitrox_Mute");
        GetTooltip(KickObject).TooltipText = GetLocalizedText("Nitrox_Kick");
        GetTooltip(TeleportToObject).TooltipText = GetLocalizedText("Nitrox_TeleportTo");
        GetTooltip(TeleportToMeObject).TooltipText = GetLocalizedText("Nitrox_TeleportToMe");
    }

    public new void Uninitialize()
    {
        // 取消延迟更新事件订阅
        if (pingManager != null)
        {
            pingManager.OnPingUpdated -= OnPingUpdated;
        }
        
        // 清理延迟显示组件
        if (pingLabel != null)
        {
            GameObject.Destroy(pingLabel.gameObject);
            pingLabel = null;
        }
        
        base.Uninitialize();
        player = null;
    }

    public void UpdateLabel(string text)
    {
        label.text = text;
        UpdatePingDisplay(); // 同时更新延迟显示
    }
    
    /// <summary>
    /// 设置延迟显示UI组件
    /// </summary>
    private void SetupPingDisplay()
    {
        Log.Info($"[PING] 检查延迟显示设置 | IsLocalPlayer: {IsLocalPlayer} | PingManager存在: {pingManager != null} | 玩家名: {player?.PlayerName}");
        
        // 为所有玩家显示延迟，但优先本地玩家
        if (pingManager != null)
        {
            // 为本地玩家或当前玩家创建延迟显示标签
            GameObject pingObject = new("PingDisplay");
            pingObject.transform.SetParent(label.transform.parent);
            
            // 设置位置（在玩家名称右侧，更靠近一些）
            RectTransform pingRect = pingObject.AddComponent<RectTransform>();
            pingRect.anchoredPosition = new Vector2(120f, 0f); // 更近的位置
            pingRect.sizeDelta = new Vector2(120f, 20f); // 稍微宽一点
            
            // 创建文本组件
            pingLabel = pingObject.AddComponent<TextMeshProUGUI>();
            pingLabel.text = "延迟: --ms";
            pingLabel.fontSize = 11f;
            pingLabel.color = Color.white; // 使用白色更醒目
            pingLabel.alignment = TextAlignmentOptions.Left;
            
            // 立即显示初始延迟信息
            UpdatePingDisplay();
            
            // 监听延迟更新事件
            pingManager.OnPingUpdated += OnPingUpdated;
            
            Log.Info($"[PING] 已为玩家 '{player?.PlayerName}' 设置延迟显示组件");
        }
        else
        {
            Log.Warn($"[PING] 无法为玩家 '{player?.PlayerName}' 设置延迟显示 - PingManager为null");
        }
    }
    
    /// <summary>
    /// 延迟更新回调
    /// </summary>
    private void OnPingUpdated(long averagePing)
    {
        if (pingLabel != null)
        {
            Log.Debug($"[PING] 收到延迟更新事件 | 玩家: {player?.PlayerName} | 平均延迟: {averagePing}ms");
            UpdatePingDisplay();
        }
    }
    
    /// <summary>
    /// 更新延迟显示
    /// </summary>
    private void UpdatePingDisplay()
    {
        if (pingLabel != null && pingManager != null)
        {
            string displayText = pingManager.GetPingDisplayText();
            pingLabel.text = displayText;
            Log.Debug($"[PING] 更新玩家 '{player?.PlayerName}' 的延迟显示: {displayText}");
        }
        else
        {
            Log.Debug($"[PING] 无法更新延迟显示 | Label存在: {pingLabel != null} | Manager存在: {pingManager != null}");
        }
    }

    public void UpdateEntryForNewPlayer(INitroxPlayer newPlayer, LocalPlayer localPlayer, IPacketSender packetSender)
    {
        player = newPlayer;

        UpdateLabel(player.PlayerName);
        Color playerColor = player.PlayerSettings.PlayerColor.ToUnity();
        icon.SetColors(playerColor, playerColor, playerColor);
        RefreshMuteButton();

        // We need to update each button's listener whether or not they have enough perms because they may become OP during playtime
        ClearButtonListeners();

        GetToggle(ShowObject).onValueChanged.AddListener(delegate (bool toggled)
        {
            if (player is RemotePlayer remotePlayer)
            {
                PingInstance pingInstance = remotePlayer.PlayerModel.GetComponentInChildren<PingInstance>();
                pingInstance.SetVisible(toggled);
                GetTooltip(ShowObject).TooltipText = GetLocalizedText(toggled ? "Nitrox_HidePing" : "Nitrox_ShowPing");
                visibilityIcon.sprite = toggled ? spriteVisible : spriteHidden;
            }
        });
        // Each of those clicks involves a confirmation modal
        GetToggle(MuteObject).onValueChanged.AddListener(delegate (bool toggled)
        {
            Modal.Get<ConfirmModal>()?.Show(GetLocalizedText(muted ? "Nitrox_Unmute" : "Nitrox_Mute", true), () =>
            {
                GetToggle(MuteObject).SetIsOnWithoutNotify(!toggled);
                if (player is RemotePlayer remotePlayer)
                {
                    packetSender.Send(new ServerCommand($"{(toggled ? "" : "un")}mute {player.PlayerName}"));
                }
            });
        });
        GetToggle(KickObject).onValueChanged.AddListener(delegate (bool toggled)
        {
            Modal.Get<ConfirmModal>()?.Show(GetLocalizedText("Nitrox_Kick", true), () =>
            {
                packetSender.Send(new ServerCommand($"kick {player.PlayerName}"));
            });
        });
        GetToggle(TeleportToObject).onValueChanged.AddListener(delegate (bool toggled)
        {
            Modal.Get<ConfirmModal>()?.Show(GetLocalizedText("Nitrox_TeleportTo", true), () =>
            {
                packetSender.Send(new ServerCommand($"warp {player.PlayerName}"));
            });
        });
        GetToggle(TeleportToMeObject).onValueChanged.AddListener(delegate (bool toggled)
        {
            Modal.Get<ConfirmModal>()?.Show(GetLocalizedText("Nitrox_TeleportToMe", true), () =>
            {
                packetSender.Send(new ServerCommand($"warp {player.PlayerName} {localPlayer.PlayerName}"));
            });
        });

        RefreshButtonsVisibility();
    }

    private string GetLocalizedText(string key, bool isQuestion = false)
    {
        return Language.main.Get(isQuestion ? $"{key}Question" : key).Replace("{PLAYER}", PlayerName);
    }

    public void UpdateButtonsPosition()
    {
        float OFFSET = 0f;
        List<GameObject> buttonsToAlign = new() { MuteObject, KickObject, TeleportToObject, TeleportToMeObject };
        foreach (GameObject buttonObject in buttonsToAlign)
        {
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, 0f);
            buttonRect.localPosition = new Vector2(OFFSET, 0);
            OFFSET += 80f;
        }
    }

    private void ClearButtonListeners()
    {
        GetToggle(MuteObject).onValueChanged = new Toggle.ToggleEvent();
        GetToggle(KickObject).onValueChanged = new Toggle.ToggleEvent();
        GetToggle(TeleportToObject).onValueChanged = new Toggle.ToggleEvent();
        GetToggle(TeleportToMeObject).onValueChanged = new Toggle.ToggleEvent();
    }

    private IEnumerator AssignSprites()
    {
        yield return new WaitUntil(() => parent.FinishedLoadingAssets);

        // NB: Those textures MUST be exported with a Texture Type of "Sprite (2D and UI)", else they will look blurry not matter what
        // NB 2: Those textures for the buttons are scaled 68x61 but the image inside but not hit the borders to have a better render
        MutedSprite = parent.GetSprite("muted@3x");
        UnmutedSprite = parent.GetSprite("unmuted@3x");
        KickSprite = parent.GetSprite("kick@3x");
        TeleportToSprite = parent.GetSprite("teleport_to@3x");
        TeleportToMeSprite = parent.GetSprite("teleport_to_me@3x");

        MuteObject.FindChild("Eye").GetComponent<Image>().sprite = muted ? MutedSprite : UnmutedSprite;
        KickObject.FindChild("Eye").GetComponent<Image>().sprite = KickSprite;
        TeleportToObject.FindChild("Eye").GetComponent<Image>().sprite = TeleportToSprite;
        TeleportToMeObject.FindChild("Eye").GetComponent<Image>().sprite = TeleportToMeSprite;
    }

    private void RefreshMuteButton()
    {
        GetToggle(MuteObject).SetIsOnWithoutNotify(muted);
        GetTooltip(MuteObject).TooltipText = GetLocalizedText(muted ? "Nitrox_Unmute" : "Nitrox_Mute");
        MuteObject.FindChild("Eye").GetComponent<Image>().sprite = muted ? MutedSprite : UnmutedSprite;
    }

    private void RefreshButtonsVisibility()
    {
        LocalPlayer localPlayer = NitroxServiceLocator.LocateService<LocalPlayer>();

        bool isNotLocalPlayer = !IsLocalPlayer;
        // We don't want any control buttons to appear for the local player
        ShowObject.SetActive(isNotLocalPlayer);

        // The perms here should be the same as the perm each command asks for
        MuteObject.SetActive(isNotLocalPlayer && localPlayer.Permissions >= Perms.MODERATOR);
        KickObject.SetActive(isNotLocalPlayer && localPlayer.Permissions >= Perms.MODERATOR);
        TeleportToObject.SetActive(isNotLocalPlayer && localPlayer.Permissions >= Perms.MODERATOR);
        TeleportToMeObject.SetActive(isNotLocalPlayer && localPlayer.Permissions >= Perms.MODERATOR);
    }

    private Toggle GetToggle(GameObject gameObject)
    {
        return gameObject.GetComponent<Toggle>();
    }

    private ButtonTooltip GetTooltip(GameObject gameObject)
    {
        return gameObject.GetComponent<ButtonTooltip>();
    }
}
