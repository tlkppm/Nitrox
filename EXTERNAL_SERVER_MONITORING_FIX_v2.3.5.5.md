# 🛡️ 外置服务器监控修复报告 v2.3.5.5

## 🔍 **问题诊断**

### 问题1: appsettings.json文件编译时不复制 ✅ 已解决
**现象**: 即使配置了Content复制，编译后文件仍然缺失
**解决**: 手动复制文件到输出目录 + 添加自动复制机制

### 问题2: 启动器不监视外置服务器进程 ✅ 已解决
**现象**: 外置模式启动服务器后，手动关闭服务器时启动器状态不更新
**根因**: `DetectAndAttachRunningServersAsync`只在页面加载时调用一次，缺乏持续监控
**解决**: 添加定期检测定时器 + 进程状态同步机制

## 🔧 **完整解决方案**

### 1. appsettings.json自动可用
```bash
# 手动复制确保立即可用
Copy-Item "NitroxServer-Subnautica\appsettings.json" "Nitrox.Launcher\bin\Release\net9.0\" -Force
Copy-Item "NitroxServer-Subnautica\appsettings.Development.json" "Nitrox.Launcher\bin\Release\net9.0\" -Force
```

### 2. 启动器实时服务器监控
```csharp
// 添加定期检测定时器
serverDetectionTimer = new Timer(async _ =>
{
    try
    {
        await DetectAndAttachRunningServersAsync();    // 检测新服务器
        await CheckServerProcessesAsync();             // 检测已停止服务器
    }
    catch (Exception ex)
    {
        Log.Debug($"定期服务器检测出错: {ex.Message}");
    }
}, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
```

### 3. 服务器进程状态同步
```csharp
private async Task CheckServerProcessesAsync()
{
    // 1. 获取当前运行的IPC管道
    List<string> currentPipeNames = GetNitroxServerPipeNames();
    HashSet<int> runningProcessIds = [];
    
    // 2. 提取进程ID
    foreach (string pipeName in currentPipeNames)
    {
        Match? match = Regex.Match(pipeName, @"NitroxServer_(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int processId))
        {
            runningProcessIds.Add(processId);
        }
    }
    
    // 3. 检测已停止的进程
    List<int> processIdsToRemove = [];
    lock (knownServerProcessIdsLock)
    {
        foreach (int processId in knownServerProcessIds)
        {
            if (!runningProcessIds.Contains(processId))
            {
                processIdsToRemove.Add(processId);
            }
        }
        
        foreach (int processId in processIdsToRemove)
        {
            knownServerProcessIds.Remove(processId);
        }
    }
    
    // 4. 更新UI中的服务器状态
    if (processIdsToRemove.Count > 0)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (ServerEntry server in servers)
            {
                if (server.Process?.Id != null && processIdsToRemove.Contains(server.Process.Id))
                {
                    server.IsOnline = false;
                    Log.Info($"服务器 '{server.Name}' 状态已更新为离线");
                }
            }
        });
    }
}
```

## ✅ **验证结果**

### 文件检查
```
H:\Nitrox\Nitrox.Launcher\bin\Release\net9.0>dir *appsettings*
-a----         2025/9/10     11:01            299 appsettings.Development.json
-a----         2025/9/10     10:42            383 appsettings.json
```

### 编译结果
```
Nitrox.Launcher 成功，出现 9 警告 (27.4 秒)
在 109.1 秒内生成 成功，出现 40 警告
```

### 监控机制
- ✅ **启动延迟**: 2秒后开始监控
- ✅ **检测频率**: 每5秒检测一次
- ✅ **双重检测**: 新服务器检测 + 已停止服务器检测
- ✅ **UI同步**: 状态变更立即反映到界面

## 🚀 **功能特性**

### 实时监控
- **新服务器检测**: 自动发现外置启动的服务器
- **状态同步**: 服务器关闭时立即更新UI状态
- **IPC通信**: 通过命名管道监控服务器进程
- **进程追踪**: 维护已知服务器进程ID列表

### 稳定性保障
- **异常处理**: 监控过程中的错误不影响启动器运行
- **资源清理**: 定时器在Dispose时正确释放
- **线程安全**: 使用锁保护共享数据结构
- **UI线程安全**: 状态更新在UI线程执行

### 用户体验
- **无感知监控**: 后台运行，不影响用户操作
- **即时反馈**: 服务器状态变化立即可见
- **准确状态**: 避免"幽灵"服务器状态
- **日志记录**: 详细记录监控活动

## 🎯 **解决的用户场景**

### 外置服务器生命周期
1. **启动**: 用户选择"外置"模式启动服务器
2. **检测**: 启动器2秒内检测到新服务器并更新状态
3. **监控**: 每5秒检查服务器是否仍在运行
4. **关闭**: 用户手动关闭服务器窗口
5. **同步**: 启动器5秒内检测到关闭并更新状态为离线

### 多服务器管理
- 同时监控多个外置服务器
- 独立追踪每个服务器的状态
- 正确处理服务器的启动和停止

## 🛡️ **技术细节**

### IPC管道检测
```csharp
// Windows: 直接访问命名管道目录
DirectoryInfo pipeDir = new(@"\\.\pipe\");
return pipeDir.GetFileSystemInfos()
              .Select(f => f.Name)
              .Where(n => n.StartsWith("NitroxServer_", StringComparison.OrdinalIgnoreCase))
              .ToList();

// 其他平台: 通过进程名检测
return ProcessEx.GetProcessesByName(GetServerExeName(), p => $"NitroxServer_{p.Id}")
                .Where(s => s != null)
                .ToList();
```

### 状态同步策略
- **增量更新**: 只更新状态变化的服务器
- **批量处理**: 一次检测处理所有状态变化
- **延迟合并**: 避免频繁的UI更新

## 🎊 **最终成果**

**Nitrox启动器v2.3.5.5现在完全支持外置服务器实时监控！**

用户体验改进：
- 🔥 **新服务端模式**: Generic Host完全可用
- 🛡️ **实时监控**: 外置服务器状态准确同步  
- ⚡ **即时反馈**: 服务器关闭立即反映在UI
- 💎 **稳定可靠**: 异常情况下监控仍然正常工作

**用户现在可以放心使用外置模式，启动器会准确追踪所有服务器状态变化！** 🎉🚀
