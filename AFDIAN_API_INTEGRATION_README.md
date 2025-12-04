# 爱发电API集成文档

## 概述
本文档说明如何在Nitrox启动器中集成并使用爱发电API来实时展示赞助者信息。

## 文件清单

### 1. 新增文件
- `Nitrox.Launcher/Models/Services/AfdianApiService.cs` - 爱发电API客户端服务

### 2. 修改文件
- `Nitrox.Launcher/ViewModels/SponsorViewModel.cs` - 更新以支持API数据
- `Nitrox.Launcher/Views/SponsorView.axaml` - 更新UI以展示API信息

## API凭据加密

为了安全起见，API Token和user_id已使用Base64编码存储：

```csharp
// 原始值（仅用于说明，实际代码中已加密）
// API Token: 3pTtwrm7cb9jMKX4HQadFR5YDfSvn8UJ
// User ID: 75c6e5f625f111ecb4c352540025c377

// 加密后存储
private const string encryptedToken = "M3BUdHdybTdjYjlqTUtYNEhRYWRGUjVZRGZTdm44VUo=";
private const string encryptedUserId = "NzVjNmU1ZjYyNWYxMTFlY2I0YzM1MjU0MDAyNWMzNzc=";
```

## 功能特性

### 1. 实时赞助者数据
- 自动从爱发电API获取最新赞助者列表
- 显示赞助者头像（自动从URL加载）
- 显示套餐名称
- 显示赞助金额和日期

### 2. 本地赞助者保留
- 保留已知的本地赞助者（如Volt_伏特）
- 本地赞助者显示在列表最前面
- 可以手动添加更多本地赞助者

### 3. 数据合并
- 自动合并本地赞助者和API赞助者
- API赞助者按金额降序排序
- 使用来源标记区分（"本地" / "爱发电"）

### 4. UI增强
- 套餐名称用金色显示
- 100元以上赞助者高亮显示
- 显示赞助总金额和赞助者数量
- 支持刷新功能

## API请求流程

1. **签名生成**
   ```
   sign = MD5(token + "params" + params + "ts" + timestamp + "user_id" + user_id)
   ```

2. **请求结构**
   ```json
   {
     "user_id": "75c6e5f625f111ecb4c352540025c377",
     "params": "{\"page\":1}",
     "ts": 1729400000,
     "sign": "生成的MD5签名"
   }
   ```

3. **响应结构**
   ```json
   {
     "ec": 200,
     "em": "成功",
     "data": {
       "list": [
         {
           "user": {
             "user_id": "xxx",
             "name": "用户名",
             "avatar": "头像URL"
           },
           "plan_title": [
             {
               "name": "套餐名称",
               "price": 10000
             }
           ],
           "total_amount": 10000,
           "last_pay_time": 1729400000
         }
       ],
       "total_count": 10,
       "total_page": 1
     }
   }
   ```

## 使用方法

### 添加本地赞助者

在 `SponsorViewModel.cs` 的 `LoadLocalSponsors()` 方法中添加：

```csharp
LocalSponsors = new List<SponsorInfo>
{
    new SponsorInfo
    {
        Name = "Volt_伏特",
        Amount = 200,
        Date = DateTime.Now.AddMonths(-2),
        Message = "感谢 Nitrox 团队的出色工作！",
        Avatar = "/Assets/Images/avatars/odif.jpg",
        AvatarBitmap = avatarBitmap,
        IsHighlighted = true,
        IsFromApi = false,
        PlanName = "早期支持者"
    },
    // 在这里添加更多本地赞助者...
};
```

### 更新API凭据

如果需要更新API Token或User ID：

1. 将新的Token/UserId转换为Base64：
   ```csharp
   string base64Token = Convert.ToBase64String(Encoding.UTF8.GetBytes("新Token"));
   string base64UserId = Convert.ToBase64String(Encoding.UTF8.GetBytes("新UserId"));
   ```

2. 更新 `AfdianApiService.cs` 中的常量：
   ```csharp
   private const string encryptedToken = "新的Base64编码Token";
   private const string encryptedUserId = "新的Base64编码UserId";
   ```

## 错误处理

服务包含完善的错误处理：

1. **API请求失败** - 返回空列表，显示本地赞助者
2. **头像加载失败** - 使用文字头像代替
3. **网络错误** - 记录日志，回退到本地数据

所有错误都会记录到日志，方便调试：
```
[AfdianAPI] 获取赞助者列表失败: xxx
[SponsorViewModel] 加载 xxx 头像失败: xxx
```

## 性能优化

1. **并行加载头像** - 使用 `Task.WhenAll` 同时加载所有头像
2. **异步加载** - 不阻塞UI线程
3. **错误容忍** - 单个头像失败不影响整体加载
4. **缓存支持** - HttpClient复用，减少连接开销

## 测试

### 本地测试

1. 关闭正在运行的Nitrox启动器
2. 编译项目：
   ```powershell
   dotnet build -c Release
   ```
3. 启动新编译的启动器
4. 进入"赞助支持"页面
5. 检查日志输出

### 预期结果

- ✅ 看到Volt_伏特（带"本地"标记）
- ✅ 看到爱发电API返回的赞助者（带"爱发电"标记）
- ✅ 正确显示头像、套餐名称、金额
- ✅ 点击刷新按钮可以重新加载

## API文档参考

- 爱发电开放平台: https://afdian.com/dashboard/dev
- 查询赞助者API: https://afdian.com/p/9c65d9cc617011ed81c352540025c377

## 注意事项

1. **API限流** - 爱发电可能有API调用频率限制，建议不要频繁刷新
2. **网络依赖** - 需要网络连接才能获取API数据，离线时只显示本地赞助者
3. **凭据安全** - 虽然使用了Base64编码，但这不是真正的加密，请勿在公开代码仓库中泄露
4. **错误恢复** - API失败时会自动回退到本地数据，确保用户始终能看到赞助者

## 维护

### 定期任务
- 检查API是否正常工作
- 更新本地赞助者名单
- 清理过期数据

### 监控指标
- API调用成功率
- 头像加载成功率
- 平均加载时间

## 版本历史

- **v1.0** (2025-10-20) - 初始版本
  - 实现爱发电API集成
  - 支持本地赞助者保留
  - 添加套餐信息展示
  - 实现凭据加密存储

## 支持

如有问题，请查看日志输出或联系开发团队。

