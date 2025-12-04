using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NitroxModel.Logger;

namespace Nitrox.Launcher.Models.Services;

public class LocalizationService
{
    private static LocalizationService? instance;
    private readonly Dictionary<string, object> strings = new();
    
    public static LocalizationService Instance => instance ??= new LocalizationService();
    
    private LocalizationService()
    {
        LoadStrings();
    }
    
    private void LoadStrings()
    {
        try
        {
            string resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Strings", "zh-CN.json");
            
            if (!File.Exists(resourcePath))
            {
                Log.Warn($"中文本地化文件未找到: {resourcePath}");
                return;
            }
            
            string jsonContent = File.ReadAllText(resourcePath);
            var jsonDoc = JsonDocument.Parse(jsonContent);
            
            LoadJsonElement(jsonDoc.RootElement, "");
            
            Log.Info("成功加载中文本地化资源");
        }
        catch (Exception ex)
        {
            Log.Error($"加载本地化资源失败: {ex.Message}");
        }
    }
    
    private void LoadJsonElement(JsonElement element, string prefix)
    {
        foreach (var property in element.EnumerateObject())
        {
            string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
            
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                LoadJsonElement(property.Value, key);
            }
            else if (property.Value.ValueKind == JsonValueKind.String)
            {
                strings[key] = property.Value.GetString() ?? "";
            }
        }
    }
    
    public string GetString(string key, string defaultValue = "")
    {
        if (strings.TryGetValue(key, out object? value) && value is string stringValue)
        {
            return stringValue;
        }
        
        Log.Debug($"本地化字符串未找到: {key}");
        return string.IsNullOrEmpty(defaultValue) ? key : defaultValue;
    }
    
    // 便捷方法
    public string Get(string key) => GetString(key, key);
    
    // 启动器字符串的便捷访问
    public static class Launcher
    {
        public static string PlayGame => Instance.Get("LauncherStrings.PlayGame");
        public static string Servers => Instance.Get("LauncherStrings.Servers");
        public static string Community => Instance.Get("LauncherStrings.Community");
        public static string Blog => Instance.Get("LauncherStrings.Blog");
        public static string Updates => Instance.Get("LauncherStrings.Updates");
        public static string Options => Instance.Get("LauncherStrings.Options");
        
        public static string Save => Instance.Get("LauncherStrings.CommonButtons.Save");
        public static string Cancel => Instance.Get("LauncherStrings.CommonButtons.Cancel");
        public static string Confirm => Instance.Get("LauncherStrings.CommonButtons.Confirm");
        public static string Delete => Instance.Get("LauncherStrings.CommonButtons.Delete");
        public static string Start => Instance.Get("LauncherStrings.CommonButtons.Start");
        public static string Stop => Instance.Get("LauncherStrings.CommonButtons.Stop");
        public static string Undo => Instance.Get("LauncherStrings.CommonButtons.Undo");
        public static string Back => Instance.Get("LauncherStrings.CommonButtons.Back");
    }
    
    // 服务器相关字符串的便捷访问
    public static class Servers
    {
        public static string Title => Instance.Get("LauncherStrings.ServersTitle");
        public static string Description => Instance.Get("LauncherStrings.ServersDescription");
        public static string CreateNewServer => Instance.Get("LauncherStrings.CreateNewServerButton");
        public static string CreateNewServerMultiplayer => Instance.Get("LauncherStrings.CreateNewServerMultiplayer");
        public static string NoServersYet => Instance.Get("LauncherStrings.NoServersYet");
        public static string CreateServerToStart => Instance.Get("LauncherStrings.CreateServerToStart");
        public static string ServerName => Instance.Get("LauncherStrings.ServerName");
        public static string ServerPassword => Instance.Get("LauncherStrings.ServerPassword");
        public static string PlayerLimit => Instance.Get("LauncherStrings.PlayerLimit");
        public static string ServerPort => Instance.Get("LauncherStrings.ServerPort");
        public static string GameMode => Instance.Get("LauncherStrings.GameMode");
        public static string WorldSeed => Instance.Get("LauncherStrings.WorldSeed");
        public static string DefaultPermissions => Instance.Get("LauncherStrings.DefaultPermissions");
        public static string AutoSaveInterval => Instance.Get("LauncherStrings.AutoSaveInterval");
        public static string ClickToChangeIcon => Instance.Get("LauncherStrings.ClickToChangeIcon");
        public static string PlayersOnline => Instance.Get("LauncherStrings.PlayersOnline");
        public static string OptionsTitle => Instance.Get("LauncherStrings.Options");
        public static string Advanced => Instance.Get("LauncherStrings.Advanced");
    }
    
    // 服务器管理字符串的便捷访问
    public static class ServerManagement  
    {
        public static string UseNewServerEngine => Instance.Get("LauncherStrings.ServerManagement.UseNewServerEngine");
        public static string NewServerEngineDescription => Instance.Get("LauncherStrings.ServerManagement.NewServerEngineDescription");
        public static string NewServerEngineTooltip => Instance.Get("LauncherStrings.ServerManagement.NewServerEngineTooltip");
        public static string ManageText => Instance.Get("LauncherStrings.ServerManagement.ManageText");
        public static string ConsoleText => Instance.Get("LauncherStrings.ServerManagement.ConsoleText");
        public static string OpenWorldFolderText => Instance.Get("LauncherStrings.ServerManagement.OpenWorldFolderText");
        public static string AdvancedSettings => Instance.Get("LauncherStrings.ServerManagement.AdvancedSettings");
        public static string RestoreBackup => Instance.Get("LauncherStrings.ServerManagement.RestoreBackup");
        public static string DeleteServer => Instance.Get("LauncherStrings.ServerManagement.DeleteServer");
        public static string StartServerMultiplayer => Instance.Get("LauncherStrings.ServerManagement.StartServerMultiplayer");
        public static string StopServer => Instance.Get("LauncherStrings.ServerManagement.StopServer");
        public static string External => Instance.Get("LauncherStrings.ServerManagement.External");
        public static string ExternalTooltip => Instance.Get("LauncherStrings.ServerManagement.ExternalTooltip");
        public static string Embedded => Instance.Get("LauncherStrings.ServerManagement.Embedded");
        public static string EmbeddedTooltip => Instance.Get("LauncherStrings.ServerManagement.EmbeddedTooltip");
        public static string Online => Instance.Get("LauncherStrings.ServerManagement.Online");
        public static string Offline => Instance.Get("LauncherStrings.ServerManagement.Offline");
        public static string Playing => Instance.Get("LauncherStrings.ServerManagement.Playing");
    }
    
    // 安全相关字符串的便捷访问
    public static class Security
    {
        public static string PirateDetectionTriggered => Instance.Get("LauncherStrings.Security.PirateDetectionTriggered");
        public static string PirateDetectionDetails => Instance.Get("LauncherStrings.Security.PirateDetectionDetails");
        public static string SteamNotRunning => Instance.Get("LauncherStrings.Security.SteamNotRunning");
        public static string SteamUserNotLoggedIn => Instance.Get("LauncherStrings.Security.SteamUserNotLoggedIn");
        public static string GameNotFoundInSteam => Instance.Get("LauncherStrings.Security.GameNotFoundInSteam");
        public static string ValidationInstructions => Instance.Get("LauncherStrings.Security.ValidationInstructions");
        public static string ValidationSuccess => Instance.Get("LauncherStrings.Security.ValidationSuccess");
    }
}
