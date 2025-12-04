using NitroxClient.GameLogic.ChatUI;
using UnityEngine.InputSystem;

namespace NitroxClient.MonoBehaviours.Gui.Input.KeyBindings.Actions;

public class ChatKeyBindingAction : KeyBinding
{
    public ChatKeyBindingAction() : base("Nitrox_Keybind_OpenChat", "y")
    {
    }

    public override void Execute(InputAction.CallbackContext _)
    {
        Log.Info($"[CHAT_DEBUG] 键绑定触发 | Multiplayer.Main={Multiplayer.Main != null} | Multiplayer.Joined={Multiplayer.Joined} | FPSInputModule.lastGroup={FPSInputModule.current.lastGroup}");
        
        // If no other UWE input field is currently active then allow chat to open.
        if (FPSInputModule.current.lastGroup == null && Multiplayer.Joined)
        {
            Log.Info("[CHAT] 条件满足，正在打开聊天...");
            PlayerChatManager.Instance.SelectChat();
        }
        else
        {
            string reason = FPSInputModule.current.lastGroup != null 
                ? "其他输入组激活中" 
                : "未加入多人游戏";
            Log.Info($"[CHAT] 聊天打开条件不满足: {reason}");
        }
    }
}
