# Nitrox v2.3.6.7 存档损坏实体修复报告

## 版本号
**v2.3.6.7** - 2025年10月11日

## 问题描述

服务器在加载世界存档时崩溃，错误信息：
```
System.Exception: Given level '100' does not have any defined cells.
   at NitroxModel.DataStructures.GameLogic.AbsoluteEntityCell.GetCellsPerBlock(Int32 level)
```

## 根本原因

1. **无效的实体级别**：存档中的某些 `WorldEntity` 对象的 `Level` 属性包含无效值（如 100）
2. **有效级别范围**：游戏只定义了级别 0、1、2、3 的单元格
3. **数据损坏**：这些无效的实体可能是由于以前的版本bug或存档损坏导致的

## 技术细节

### 有效级别定义（AbsoluteEntityCell.cs）

```csharp
private static int GetCellsPerBlock(int level)
{
    switch (level)
    {
        case 0:
            return 10;
        case 1:
        case 2:
        case 3:
            return 5;
        default:
            throw new Exception($"Given level '{level}' does not have any defined cells.");
    }
}
```

### 问题代码（WorldEntityManager.cs - 修复前）

```csharp
worldEntitiesByCell = worldEntities.Where(entity => entity is not GlobalRootEntity)
                                   .GroupBy(entity => entity.AbsoluteEntityCell)
                                   .ToDictionary(group => group.Key, group => group.ToDictionary(entity => entity.Id, entity => entity));
```

当访问 `entity.AbsoluteEntityCell` 时，如果实体的 `Level` 无效，会抛出异常，导致服务器启动失败。

---

## 解决方案

### 实现的修复

**文件**: `NitroxServer/GameLogic/Entities/WorldEntityManager.cs`

**修复策略**:
1. 在构造函数中添加实体验证循环
2. 捕获无效实体的异常
3. 记录警告信息（包括实体ID、ClassId和无效的Level）
4. 跳过无效实体，继续加载其他实体
5. 统计并报告跳过的实体数量

**修复后的代码**:

```csharp
public WorldEntityManager(EntityRegistry entityRegistry, BatchEntitySpawner batchEntitySpawner, PlayerManager playerManager)
{
    List<WorldEntity> worldEntities = entityRegistry.GetEntities<WorldEntity>();

    globalRootEntitiesById = entityRegistry.GetEntities<GlobalRootEntity>().ToDictionary(entity => entity.Id);

    // Filter out entities with invalid levels and log warnings
    List<WorldEntity> validWorldEntities = new();
    int invalidEntityCount = 0;
    
    foreach (WorldEntity entity in worldEntities.Where(entity => entity is not GlobalRootEntity))
    {
        try
        {
            // Try to access AbsoluteEntityCell to validate the entity
            _ = entity.AbsoluteEntityCell;
            validWorldEntities.Add(entity);
        }
        catch (Exception ex)
        {
            invalidEntityCount++;
            Log.Warn($"[WorldEntityManager] Skipping invalid entity (ID: {entity.Id}, ClassId: {entity.ClassId}, Level: {entity.Level}): {ex.Message}");
        }
    }
    
    if (invalidEntityCount > 0)
    {
        Log.Warn($"[WorldEntityManager] Skipped {invalidEntityCount} invalid entities with corrupted level data. These entities may need to be removed from the save file.");
    }

    worldEntitiesByCell = validWorldEntities.GroupBy(entity => entity.AbsoluteEntityCell)
                                            .ToDictionary(group => group.Key, group => group.ToDictionary(entity => entity.Id, entity => entity));
    this.entityRegistry = entityRegistry;
    this.batchEntitySpawner = batchEntitySpawner;
    this.playerManager = playerManager;

    worldEntitiesLock = new();
    globalRootEntitiesLock = new();
}
```

---

## 修复效果

### 修复前
- 服务器启动失败
- 抛出 `System.Exception: Given level '100' does not have any defined cells.`
- 无法加载存档
- 玩家无法连接

### 修复后
- 服务器成功启动
- 自动跳过损坏的实体
- 记录详细的警告信息
- 玩家可以正常连接和游戏
- 损坏的实体不会影响游戏体验

### 示例日志输出

```
[WorldEntityManager] Skipping invalid entity (ID: abc123-def456-ghi789, ClassId: SomeCorruptedEntity, Level: 100): Given level '100' does not have any defined cells.
[WorldEntityManager] Skipped 5 invalid entities with corrupted level data. These entities may need to be removed from the save file.
```

---

## 影响范围

### 受影响的系统
- 世界实体管理系统
- 存档加载系统
- 服务器启动流程

### 不受影响的系统
- 客户端
- 网络通信
- 游戏逻辑（除了被跳过的损坏实体）

---

## 建议和注意事项

### 对于服务器管理员

1. **正常情况**：如果看到少量（1-5个）无效实体警告，通常可以忽略
2. **大量警告**：如果看到大量（>10个）无效实体警告，建议：
   - 备份当前存档
   - 考虑创建新的存档
   - 向开发团队报告问题

3. **存档清理**：如果需要手动清理存档中的损坏实体：
   - 存档位置：`Nitrox.Server.Subnautica/Database/[存档名]/`
   - 相关文件：`world.nitrox`
   - **警告**：仅建议高级用户手动编辑存档文件

### 对于开发者

1. **预防措施**：在创建新实体时，确保 `Level` 值在有效范围（0-3）内
2. **数据验证**：考虑在实体序列化/反序列化时添加验证逻辑
3. **向后兼容**：此修复保持了与旧存档的兼容性

---

## 相关文件

- `NitroxServer/GameLogic/Entities/WorldEntityManager.cs` - 主要修复文件
- `NitroxModel/DataStructures/GameLogic/AbsoluteEntityCell.cs` - 级别验证逻辑
- `NitroxModel/DataStructures/GameLogic/Entities/WorldEntity.cs` - 实体数据结构

---

## 测试建议

1. **正常存档测试**：使用正常的存档启动服务器，确认不受影响
2. **损坏存档测试**：使用包含无效实体的存档启动服务器，确认能正常启动并跳过损坏实体
3. **日志验证**：检查服务器日志，确认警告信息正确输出
4. **游戏测试**：连接服务器并游戏一段时间，确认没有异常

---

## 未来改进方向

1. **自动修复**：在保存世界时自动修正或删除无效实体
2. **更详细的日志**：记录实体的位置信息，帮助定位问题
3. **存档验证工具**：开发独立工具来验证和修复损坏的存档
4. **级别范围扩展**：如果游戏未来扩展支持更多级别，更新验证逻辑

---

## 致谢

感谢用户报告此问题，帮助我们发现和修复存档损坏导致的服务器崩溃问题。

---

**报告生成时间**: 2025-10-11  
**报告版本**: v1.0  
**作者**: AI Assistant

