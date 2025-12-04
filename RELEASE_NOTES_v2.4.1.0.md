# Nitrox v2.4.1.0 发布公告

## 版本信息
- **版本号**: 2.4.1.0
- **发布日期**: 2025年10月20日
- **更新类型**: 关键Bug修复与功能增强

---

## 更新概述

本次为紧急修复更新，同步官方Nitrox仓库的两个关键PR（#2535和#2541），解决了多个影响游戏体验的严重问题，并新增了服务器管理功能。强烈建议所有玩家立即更新！

---

##  关键修复内容

### 1. 建造系统修复（PR #2535）
**问题描述**: 建造物销毁包未正确广播，导致每次进入存档时都会看到重复的建筑物、存储柜或模块

**修复内容**:
-  修复 `Constructable_Construct_Patch.cs` 中建造物销毁时包不发送的bug
-  当建造物被完全拆除（amount == 0f）时，立即发送销毁包而不是使用节流机制
-  确保所有客户端都能同步看到建筑物的移除操作
-  彻底解决存储柜/模块重复出现的异常问题

**影响**: 这是一个严重的游戏破坏性bug，会导致玩家存档中出现大量重复物体，现已完全修复

---

### 2. 实体生成系统修复（PR #2541）
**问题描述**: 空单元格信息未发送给客户端，导致泡泡鱼等生物在同一位置异常聚集刷新

**修复内容**:
-  修改 `SpawnEntities` 包结构，新增 `SpawnedCells` 属性
-  修复 `CellVisibilityChangedProcessor.cs`，确保即使单元格为空也会发送信息
-  更新 `SpawnEntitiesProcessor.cs` 和 `Entities.cs`，正确处理空单元格数据
-  新增 `Terrain.cs` 中的 `fullySpawnedCells` 追踪机制
-  优化单元格加载机制，防止生物重复生成

**影响**: 解决了多人游戏中最常见的生物异常刷新问题，实体生成现在更加稳定可靠

---

### 3. 载具物理修复（PR #2541）
**问题描述**: 外骨骼和海蛾在生成时会穿地掉落，严重影响游戏体验

**修复内容**:
-  在 `VehicleEntitySpawner.cs` 中添加 `constructionFallOverride = false`
-  防止载具在生成后像刚从建造器中制造出来一样自由下落
-  确保载具在生成后保持正确的物理状态

**影响**: 载具生成后不再异常坠落穿过地形，多人游戏中的载具同步更加稳定

---

### 4. 生物同步修复（新增）
**问题描述**: 生物死亡后，不同客户端看到的尸体位置不一致

**修复内容**:
-  修改 `CreatureDeath_OnKillAsync_Patch.cs`，发送包时使用世界坐标（`transform.position`）
-  修改 `RemoveCreatureCorpseProcessor.cs`，接收包时应用世界坐标
-  替换原本使用的本地坐标（`localPosition/localRotation`）为世界坐标

**影响**: 确保所有客户端看到的生物尸体位置完全一致，提升多人游戏的同步质量

---

##  新增功能

### 服务器管理增强
**新功能**: 在服务器管理页面新增"禁用游戏指令框"选项

**具体内容**:
-  添加 `ServerDisableConsole` 配置属性
-  可独立控制游戏内置控制台（F3/Enter）的启用状态
-  设置会正确保存到 `server.cfg` 并在服务器启动时应用
-  提供更灵活的服务器管理选项

**使用场景**: 
- 服务器管理员可以禁用游戏内控制台，同时保留Nitrox聊天功能
- 防止玩家误操作或滥用游戏内指令
- 提供更细粒度的权限控制

---

##  技术细节

### 修改的文件列表

**建造系统**:
- `NitroxPatcher/Patches/Dynamic/Constructable_Construct_Patch.cs`

**实体生成系统**:
- `NitroxModel/Packets/SpawnEntities.cs`
- `NitroxServer/Communication/Packets/Processors/CellVisibilityChangedProcessor.cs`
- `NitroxClient/Communication/Packets/Processors/SpawnEntitiesProcessor.cs`
- `NitroxClient/GameLogic/Entities.cs`
- `NitroxClient/GameLogic/Terrain.cs`

**载具系统**:
- `NitroxClient/GameLogic/Spawning/WorldEntities/VehicleEntitySpawner.cs`

**生物同步**:
- `NitroxPatcher/Patches/Dynamic/CreatureDeath_OnKillAsync_Patch.cs`
- `NitroxClient/Communication/Packets/Processors/RemoveCreatureCorpseProcessor.cs`

**服务器管理**:
- `Nitrox.Launcher/Models/Design/ServerEntry.cs`
- `Nitrox.Launcher/ViewModels/ManageServerViewModel.cs`
- `Nitrox.Launcher/Views/ManageServerView.axaml`
- `NitroxModel/Serialization/SubnauticaServerConfig.cs`

**版本管理**:
- `Directory.Build.props`
- `Nitrox.Launcher/Models/Services/AnnouncementService.cs`

---

##  安装说明

### 首次安装
1. 下载 `Nitrox-v2.4.1.0-Release.zip`
2. 解压到任意目录
3. 运行 `Nitrox.Launcher.exe`
4. 按照启动器指引选择游戏路径
5. 创建或加入服务器

### 从旧版本升级
1. **备份您的存档**（推荐）
   - 存档位置: `%APPDATA%\Nitrox\saves`
2. 下载新版本并覆盖安装
3. 启动启动器，所有设置将自动保留

### 服务器升级
1. **停止现有服务器**
2. **备份服务器存档**（重要！）
   - 存档位置: `%APPDATA%\Nitrox\saves\[服务器名称]`
3. 用新版本 `NitroxServer-Subnautica.exe` 替换旧版本
4. 重启服务器

---

##  重要提示

### 兼容性说明
-  本版本完全兼容 Subnautica 主游戏最新版
-  向下兼容 v2.4.0.0 的存档
-  建议所有服务器和客户端同时更新到 v2.4.1.0

### 已知问题
-  .NET Generic Host 聊天框 Y 键快捷键问题正在调查中
-  部分玩家报告的性能问题正在优化

### 备份建议
由于本次更新涉及实体生成和建造系统的核心修改，**强烈建议**在更新前备份您的存档：
```
备份路径: %APPDATA%\Nitrox\saves
```

---

##  致谢

- 感谢 Nitrox 官方团队的 PR #2535 和 #2541
- 感谢社区玩家提供的bug反馈和测试
- 特别感谢报告泡泡鱼异常刷新和建筑物重复问题的玩家

---

## 📞 支持与反馈

如果您在使用过程中遇到问题，请通过以下方式反馈：
- GitHub Issues: [SubnauticaNitrox/Nitrox](https://github.com/SubnauticaNitrox/Nitrox)
- Discord 社区服务器
- 中文社区 QQ群

---

##  更新日志摘要

```
v2.4.1.0 (2025-10-20)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

 关键修复
  - 修复建造物销毁包不广播的bug（PR #2535）
  - 修复空单元格信息不发送导致生物异常刷新（PR #2541）
  - 修复载具穿地掉落问题（PR #2541）
  - 修复生物死亡后尸体位置不同步问题

 新增功能
  - 服务器管理页面新增"禁用游戏指令框"选项

 改进
  - 优化建造物销毁时的包发送逻辑
  - 优化单元格加载机制
  - 改进地形加载和实体生成稳定性

 技术
  - 修改了 15 个核心文件
  - 新增 SpawnedCells 包属性
  - 统一版本号到 2.4.1.0
```

---

**再次提醒**: 本次更新修复了多个影响游戏体验的关键bug，**强烈建议所有玩家立即更新**！

祝您在深海迷航的多人世界中玩得愉快！

