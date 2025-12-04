# Nitrox v2.4.6.1 优化更新 🔧

## ✨ 主要优化

### 1. **Generic Host 默认启用** ✅
- ✅ 将 `UseGenericHost` 默认值从 `false` 改为 `true`
- ✅ 新创建的服务器自动使用 Generic Host 引擎
- ✅ 享受更好的性能和架构优势
- ✅ Web API 自动可用，支持真实玩家名称显示

**影响范围**：
- **新服务器**：自动使用 Generic Host
- **现有服务器**：保持原有设置不变，可手动在服务器管理页面勾选启用

### 2. **文档修正** ✅
- ✅ 修正了"禁用游戏指令框"选项的快捷键说明
- ✅ 正确按键：`（· ` ~）`
- ✅ 之前错误显示：`（F3/Enter）`

---

## 📋 修改的文件

1. ✅ `NitroxModel/Serialization/SubnauticaServerConfig.cs`
   ```csharp
   // 修改前
   public bool UseGenericHost { get; set; } = false;
   
   // 修改后
   public bool UseGenericHost { get; set; } = true;
   ```

2. ✅ `Nitrox.Launcher/Views/ManageServerView.axaml`
   ```xml
   <!-- 修改前 -->
   <TextBlock Text="禁用游戏内置的控制台指令框（F3/Enter）" />
   
   <!-- 修改后 -->
   <TextBlock Text="禁用游戏内置的控制台指令框（· ` ~）" />
   ```

3. ✅ `Nitrox.Launcher/Models/Services/AnnouncementService.cs` - 新增公告
4. ✅ `Directory.Build.props` - 版本号：2.4.6.0 → 2.4.6.1

---

## 🎯 对比表

| 项目 | v2.4.6.0 | v2.4.6.1 |
|------|----------|----------|
| Generic Host 默认值 | ❌ 关闭 | ✅ 开启 |
| 控制台快捷键说明 | ❌ （F3/Enter） | ✅ （· ` ~） |
| 新服务器体验 | 需手动启用 | 自动启用 |

---

## 💡 使用说明

### 对于新用户
1. 创建服务器时，"使用新服务器引擎(通用主机)"默认已勾选
2. 直接创建即可享受 Generic Host 的所有优势
3. 外部模式可以显示真实玩家名称

### 对于现有用户
1. 现有服务器保持原有设置
2. 如需升级到 Generic Host：
   - 打开服务器管理页面
   - 勾选"使用新服务器引擎(通用主机)"
   - 保存设置
   - 重启服务器

### 关于游戏控制台
- 游戏内控制台按键：`· ` 或 `~`（通常在键盘左上角，ESC键下方）
- "禁用游戏指令框"选项：禁用游戏内置的控制台
- Nitrox服务器控制台：不受此选项影响，始终可用

---

## 🔧 技术细节

### 默认值更改
```csharp
// SubnauticaServerConfig.cs 第 137 行
[PropertyDescription("启用后使用 .NET Generic Host 以改进服务器架构和性能")]
public bool UseGenericHost { get; set; } = true; // 改为 true
```

### 文本修正
```xml
<!-- ManageServerView.axaml 第 413 行 -->
<TextBlock Text="禁用游戏内置的控制台指令框（· ` ~）" 
           FontSize="12" 
           Opacity="0.7" 
           Margin="0,2,0,0" />
```

---

## 🎉 Generic Host 优势回顾

使用 Generic Host 引擎的服务器可以享受：

1. **真实玩家信息** - Web API 支持，外部模式显示真实玩家名称
2. **更好的性能** - 优化的服务器架构
3. **现代化设计** - 基于 .NET 最佳实践
4. **API 接口** - 为未来功能扩展奠定基础
5. **完全兼容** - 与旧版客户端完全兼容

---

**版本**: v2.4.6.1  
**发布日期**: 2025-10-26  
**编译状态**: ✅ 成功（167.9秒，80个非关键警告）

---

## 📊 统计数据

- **编译时间**: 167.9秒
- **警告数量**: 80个（全部非关键）
- **错误数量**: 0个
- **修改文件**: 4个
- **影响范围**: 新服务器创建流程 + UI文本修正

---

## 🙏 致谢

感谢用户反馈帮助我们改进文档准确性和用户体验！

