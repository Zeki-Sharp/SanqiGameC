# 视效控制器使用说明

## 🎯 系统概述

视效控制器是一个**基础特效库 + 自由组合配置**的系统，通过ScriptableObject配置各种特效组合，实现灵活的视觉效果管理。

## 🏗️ 清晰的架构层次

### **第一层：基础特效库**
- `FlashEffect` - 基础闪烁特效（独立的MonoBehaviour组件）
- `KnockbackEffect` - 基础击退特效（独立的MonoBehaviour组件）
- `ScaleEffect` - 基础缩放特效（独立的MonoBehaviour组件）

### **第二层：特效组合配置**
- `EffectConfig` - 单个特效的配置数据
- `EffectCombination` - 特效组合（如：击退+闪烁变白）
- `EffectCombinationPreset` - 特效组合预设文件

### **第三层：控制器**
- `VisualEffectController` - 负责播放特效组合（自动注册到GameManager）

## 🚀 快速开始

### 1. 添加组件到GameObject
- 在需要特效的对象上添加 `VisualEffectController` 组件
- 配置 `effectPreset` 引用

### 2. 创建特效组合预设
- 在Project窗口中右键 → Create → Tower Defense → Effect Combination Preset
- 配置各种特效组合

### 3. 自动播放
- 当对象受到伤害时，系统会自动播放对应的特效组合

## ⚙️ 配置说明

### 基础特效参数
- **闪烁特效**: intensity（强度）、frequency（频率）、flashColor（颜色）
- **击退特效**: force（力度）、knockbackDuration（击退持续时间）
- **缩放特效**: targetScale（目标缩放）、elasticReturn（是否弹性返回）

### 特效组合配置
- **敌人受击**: 击退 + 闪烁变白
- **塔受击**: 闪烁变红
- **技能特效**: 闪烁变橙 + 缩放
- **状态特效**: 闪烁变绿

## 🔧 使用方法

### 基础使用（推荐）
```csharp
// 在DamageTaker中自动调用，无需手动代码
// 只需要配置好VisualEffectController和effectPreset即可
```

### 播放特效组合
```csharp
// 播放特效组合
visualEffectController.PlayEffect("EnemyHit");
visualEffectController.PlayEffect("TowerHit");
visualEffectController.PlayEffect("Fireball");
```

### 播放自定义组合
```csharp
// 播放自定义特效组合
var customCombination = new EffectCombination();
customCombination.effects.Add(new EffectConfig { effectType = EffectType.Flash });
visualEffectController.PlayEffectCombination(customCombination);
```

### 获取可用特效
```csharp
// 获取所有可用的特效组合名称
var availableEffects = visualEffectController.GetAvailableEffects();
foreach (var effectName in availableEffects)
{
    Debug.Log($"可用特效: {effectName}");
}
```

## 📁 文件结构

- `BaseVisualEffect.cs` - 基础特效基类和实现
- `EffectCombination.cs` - 特效组合配置系统
- `VisualEffectController.cs` - 主控制器（自动注册到GameManager）

## 🔗 系统集成

### GameManager统一管理
- `VisualEffectController` 自动注册到 `GameManager`
- 通过 `GameManager.Instance.VisualEffectController` 全局访问

### 特效类型支持
- **基础特效**: 闪烁、击退、缩放
- **自由组合**: 可以任意组合基础特效
- **统一接口**: 所有特效都通过 `PlayEffect(string combinationName)` 调用

## ⚠️ 注意事项

1. 确保对象有 `SpriteRenderer` 组件（用于闪烁效果）
2. 敌人对象需要设置 "Enemy" 标签
3. 特效组合预设文件需要正确配置
4. 基础特效组件会自动管理生命周期
5. 系统自动注册到 `GameManager`，无需手动管理

## 🔮 扩展性

系统设计为高度可扩展：
- **添加新基础特效**: 继承 `BaseVisualEffect` 并实现抽象方法
- **自由组合**: 通过配置任意组合基础特效
- **参数配置**: 所有特效参数都可以在ScriptableObject中配置

## 📝 配置示例

### 敌人受击效果组合
```
combinationName: "EnemyHit"
executionMode: Parallel  // 并行执行：击退和闪烁同时发生
effects:
  - effectType: Knockback
    force: 2.0
    knockbackDuration: 0.3
    duration: 0.4
  
  - effectType: Flash
    intensity: 0.8
    frequency: 8
    flashColor: white
    duration: 0.4
```

### 塔受击效果组合
```
combinationName: "TowerHit"
executionMode: Parallel  // 并行执行：只有闪烁效果
effects:
  - effectType: Flash
    intensity: 0.6
    frequency: 6
    flashColor: red
    duration: 0.3
```

### 技能特效组合（顺序执行）
```
combinationName: "Fireball"
executionMode: Sequential  // 顺序执行：先闪烁，再缩放
effects:
  - effectType: Flash
    intensity: 1.0
    frequency: 12
    flashColor: orange
    duration: 1.0
  
  - effectType: Scale
    targetScale: (1.5, 1.5, 1.5)
    elasticReturn: true
    duration: 1.0
```

## 🎨 配置流程

1. **创建基础特效**: 在GameObject上添加基础特效组件
2. **配置特效参数**: 调整基础特效的参数
3. **创建组合预设**: 创建 `EffectCombinationPreset` 文件
4. **配置特效组合**: 在预设中配置各种特效组合
5. **应用到控制器**: 将预设分配给 `VisualEffectController`
6. **自动播放**: 系统根据配置自动播放特效组合

## 💡 设计理念

### **为什么移除分类？**
- **基础特效可以自由组合**，分类没有实际意义
- **统一接口更简洁**，`PlayEffect("EffectName")` 即可
- **配置驱动**，通过命名约定来区分用途（如：EnemyHit、TowerHit、Fireball等）

### **命名约定建议**
- `EnemyHit` - 敌人受击效果
- `TowerHit` - 塔受击效果  
- `Fireball` - 火球技能效果
- `Poison` - 中毒状态效果
- `Heal` - 治疗效果

## 🔄 特效执行关系详解

### **一个组合内的特效执行方式**

#### **并行执行 (Parallel) - 默认方式**
- 所有特效同时开始执行
- 适合：受击效果（击退+闪烁同时发生）
- 示例：敌人受击时，击退和闪烁同时进行

#### **顺序执行 (Sequential)**
- 特效按配置顺序依次执行
- 适合：技能连击、状态变化序列
- 示例：火球技能先闪烁，再缩放

### **多个组合的关系**
- **每个组合都是独立调用的**
- **不是顺序执行关系**
- 调用方式：
```csharp
// 这些是独立的调用，不是顺序执行
visualEffectController.PlayEffect("EnemyHit");   // 执行EnemyHit组合
visualEffectController.PlayEffect("TowerHit");  // 执行TowerHit组合
```

### **配置建议**
- **受击效果**: 使用 `Parallel` 执行方式
- **技能效果**: 根据需求选择 `Parallel` 或 `Sequential`
- **状态效果**: 通常使用 `Sequential` 执行方式

## 🚨 配置文件问题排查

### **如果配置文件显示"The associated script can not be loaded"**

1. **检查编译错误**
   - 查看Console窗口是否有编译错误
   - 确保所有脚本都能正常编译

2. **检查脚本引用**
   - 确保ScriptableObject文件引用了正确的脚本
   - 如果脚本被重命名，需要重新创建ScriptableObject

3. **重新创建配置文件**
   - 删除有问题的配置文件
   - 重新创建 `Effect Combination Preset`

4. **检查文件路径**
   - 确保脚本文件在正确的目录中
   - 检查是否有文件重命名或移动

这样既保持了灵活性，又通过命名约定提供了清晰的使用指导。
