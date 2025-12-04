using System;
using System.ComponentModel;

namespace NitroxModel.DataStructures.GameLogic
{
    public enum Perms : byte
    {
        /// <summary>
        /// No permissions
        /// </summary>
        [Description("无权限")]
        NONE,
        /// <summary>
        /// Default player permission, cannot use cheat and have access to basic server commands (e.g: help, list, whisper, whois, ...)
        /// </summary>
        [Description("玩家")]
        PLAYER,
        /// <summary>
        /// Player that can manage other players in game. Can use vanilla cheat commands and some advanced server commands (e.g: mute, kick, broadcast, ...)
        /// </summary>
        [Description("管理员")]
        MODERATOR,
        /// <summary>
        /// Server administrator, can manage server settings and players. Can use vanilla cheat commands and all server commands (e.g: op, promote, server settings, ...)
        /// </summary>
        [Description("超级管理员")]
        ADMIN,
        /// <summary>
        /// All permissions
        /// </summary>
        [Description("控制台")]
        CONSOLE
    }

    [Flags]
    public enum PermsFlag : byte
    {
        NONE = 0x0,
        NO_CONSOLE = 0x1
    }
}
