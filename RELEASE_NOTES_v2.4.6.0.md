# Nitrox v2.4.6.0 发布说明 🚀

## ✨ 重大更新：Generic Host Web API 集成

### 核心功能
- ✅ 为 .NET Generic Host 服务端构建完整的 Web API 系统
- ✅ 启动器可通过 HTTP API 获取真实玩家信息
- ✅ 外部模式不再显示"玩家 1"占位符，显示真实玩家名称
- ✅ API 端点：`http://localhost:{游戏端口+1000}/api/players`
- ✅ 服务器状态端点：`http://localhost:{游戏端口+1000}/api/players/status`

### 技术架构

#### 1. **服务端 API 控制器**
```csharp
// PlayersController 提供玩家信息查询
GET /api/players         // 获取玩家列表
GET /api/players/status  // 获取服务器状态
```

**返回数据示例**：
```json
{
  "success": true,
  "count": 2,
  "players": [
    {
      "id": 1,
      "name": "张三",
      "permissions": "PLAYER",
      "gameMode": "SURVIVAL"
    },
    {
      "id": 2,
      "name": "李四",
      "permissions": "ADMIN",
      "gameMode": "SURVIVAL"
    }
  ],
  "serverTime": "2025-10-26T12:00:00"
}
```

#### 2. **API Host 集成**
- Web API 在 Generic Host 启动成功后自动启动
- 使用独立端口（游戏端口 + 1000），避免冲突
- 配置了 CORS 支持，允许跨域访问
- 最小化日志输出，只记录警告和错误

#### 3. **启动器智能调用**
- **Generic Host 模式**：通过 HTTP API 获取真实玩家信息
- **嵌入式模式**：继续使用 IPC 通信
- **旧版服务端**：显示占位符
- **API 失败降级**：自动回退到占位符模式

### 🎯 使用场景对比

| 服务器模式 | 数据来源 | 显示内容 | 优势 |
|-----------|---------|---------|------|
| Generic Host (外部) | Web API | 真实玩家名称 | ✅ 完整信息 |
| 嵌入式 | IPC 命令 | 真实玩家名称 | ✅ 直接通信 |
| 旧版外部 | 无 | 占位符 | ⚠️ 兼容性 |

---

## 🔧 技术细节

### NuGet 包依赖
```xml
<!-- 新增 ASP.NET Core 支持 -->
<PackageReference Include="Microsoft.AspNetCore.App" />
```

### API 启动日志
```
[DEBUG] 开始启动 Web API 服务
Web API 已启动，监听端口: 12000
[API] Web API 已启动在 http://localhost:12000
[API] 玩家列表端点: http://localhost:12000/api/players
```

### 启动器调用逻辑
```csharp
// 检查是否使用 Generic Host
if (ServerEntry?.UseGenericHost == true)
{
    int apiPort = ServerEntry.ServerPort + 1000;
    string apiUrl = $"http://localhost:{apiPort}/api/players";
    var response = await httpClient.GetFromJsonAsync<ApiPlayerListResponse>(apiUrl);
    // 显示真实玩家信息
}
```

---

## 📋 文件修改清单

### 服务端
1. **NitroxServer-Subnautica/NitroxServer-Subnautica.csproj**
   - 添加 `Microsoft.AspNetCore.App` 包引用

2. **NitroxServer-Subnautica/Api/PlayersController.cs** ✨ 新增
   - 创建 RESTful API 控制器
   - 实现 `GetPlayers()` 和 `GetServerStatus()` 方法
   - 从 DI 容器获取 `PlayerManager` 和 `Server` 实例

3. **NitroxServer-Subnautica/Program.cs**
   - 添加 `StartWebApiHostAsync()` 方法
   - 在 `StartServerWithGenericHostAsync()` 中启动 Web API
   - 配置 CORS 和路由

### 启动器
4. **Nitrox.Launcher/ViewModels/PlayerListViewModel.cs**
   - 添加 `HttpClient` 支持
   - 实现 API 调用逻辑
   - 添加 `ApiPlayerListResponse` 数据模型
   - 智能降级机制

5. **Nitrox.Launcher/Models/Services/AnnouncementService.cs**
   - 添加 v2.4.6.0 功能公告

6. **Directory.Build.props**
   - 版本号更新：2.4.5.0 → 2.4.6.0

---

## 🎉 用户体验提升

### 修复前（v2.4.5.0）
```
外部服务器模式
├── 玩家 1 - 外部服务器模式
├── 玩家 2 - 外部服务器模式
└── 玩家 3 - 外部服务器模式
```

### 修复后（v2.4.6.0）
```
Generic Host 外部模式
├── 张三 - 在线中
├── 李四 - 在线中
└── 王五 - 在线中
```

---

## ⚡ 性能与安全

### 性能优化
- HTTP 请求超时：5秒
- API 端口独立，不影响游戏服务器
- 异步调用，不阻塞 UI 线程

### 安全考虑
- API 仅监听 `localhost`，不对外网开放
- CORS 配置允许本地访问
- 错误信息不暴露敏感数据

---

## 🚀 未来扩展

Web API 架构为以下功能奠定基础：
- 远程服务器管理
- 玩家踢出/权限管理
- 服务器性能监控
- 实时日志查看
- 自动化运维工具

---

## 🛠 开发者说明

### API 端点测试
```bash
# 获取玩家列表
curl http://localhost:12000/api/players

# 获取服务器状态
curl http://localhost:12000/api/players/status
```

### 添加新端点
```csharp
[HttpGet("custom")]
public IActionResult CustomEndpoint()
{
    // 从 DI 容器获取服务
    var service = NitroxServiceLocator.LocateService<YourService>();
    return Ok(new { data = service.GetData() });
}
```

---

**版本**: v2.4.6.0  
**发布日期**: 2025-10-26  
**编译状态**: ✅ 成功（129.6秒，80个非关键警告）

---

## 💡 使用说明

1. 启用 Generic Host：在服务器设置中勾选"使用新服务器引擎(通用主机)"
2. 启动服务器，查看控制台确认 API 启动成功
3. 在服务器列表点击"玩家列表"按钮
4. 现在可以看到真实玩家名称（而不是"玩家 1"）

---

## 🎊 致谢

感谢所有测试和反馈的用户！这次更新大幅提升了外部模式的可用性。

