# Visual Studio 项目加载失败解决方案

## 问题描述

Visual Studio 显示以下错误：
```
找不到指定的 SDK"Microsoft.NET.Sdk"
```

## 解决方案

### 方法1：清理缓存（推荐）

在项目根目录执行：

```powershell
# 删除 Visual Studio 缓存
Remove-Item -Recurse -Force .vs -ErrorAction SilentlyContinue

# 清理项目输出
& 'C:\Program Files\dotnet\dotnet.exe' clean
```

### 方法2：重新加载解决方案

1. 关闭 Visual Studio
2. 执行方法1的命令
3. 重新打开 Visual Studio
4. 右键点击解决方案 → **重新加载项目**

### 方法3：全局重置

如果以上方法无效，尝试重新安装 .NET SDK：

```powershell
# 检查 .NET SDK 版本
& 'C:\Program Files\dotnet\dotnet.exe' --list-sdks

# 如果缺少 SDK，下载并安装
# https://dotnet.microsoft.com/download
```

## 编译命令

如果 IDE 无法编译，请使用完整路径命令：

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build -c Release Nitrox.Launcher
```

## 注意事项

- **不要使用** `dotnet build`（不带完整路径），会导致 SDK 找不到错误
- **必须使用** `& 'C:\Program Files\dotnet\dotnet.exe'` 完整路径
- 项目使用 .NET 9.0 RC 预览版，确保已安装

## 验证

编译成功后应看到：

```
在 XX 秒内生成 成功，出现 XX 警告
```

IDE 应能正常加载所有项目，不再显示"加载失败"标记。

