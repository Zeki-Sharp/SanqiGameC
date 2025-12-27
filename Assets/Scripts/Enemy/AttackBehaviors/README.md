# 敌人攻击行为系统

## 概述

这个系统为敌人提供了可配置的攻击行为，支持近战和远程两种攻击方式。系统采用策略模式设计，可以轻松扩展新的攻击类型。

## 系统架构

### 核心组件

1. **IAttackBehavior** - 攻击行为接口
2. **MeleeAttackBehavior** - 近战攻击行为
3. **RangedAttackBehavior** - 远程攻击行为
4. **EnemyBullet** - 敌人子弹组件
5. **AttackBehaviorFactory** - 攻击行为工厂

### 集成组件

- **EnemyData** - 敌人数据配置
- **EnemyController** - 敌人控制器
- **EnemyAttackState** - 攻击状态

## 使用方法

### 1. 创建攻击行为资源

#### 近战攻击行为
1. 在Project窗口中右键
2. 选择 `Create > Tower Defense > Attack Behaviors > Melee Attack`
3. 配置攻击参数：
   - Damage: 伤害值
   - Attack Cooldown: 攻击冷却时间
   - Attack Range: 攻击范围
   - Attack Animation Trigger: 攻击动画触发器
   - Hit Effect Prefab: 命中特效预制体
   - Attack Sound: 攻击音效

#### 远程攻击行为
1. 在Project窗口中右键
2. 选择 `Create > Tower Defense > Attack Behaviors > Ranged Attack`
3. 配置攻击参数：
   - Damage: 伤害值
   - Attack Cooldown: 攻击冷却时间
   - Attack Range: 攻击范围
   - Bullet Speed: 子弹速度
   - Bullet Prefab: 子弹预制体
   - Attack Sound: 攻击音效
   - Bullet Hit Sound: 子弹击中音效

### 2. 配置敌人数据

1. 选择敌人的EnemyData资源
2. 在Inspector中找到"攻击配置"部分
3. 将创建的攻击行为拖拽到"Attack Behavior"字段

### 3. 创建子弹预制体（远程攻击）

1. 创建一个新的GameObject
2. 添加以下组件：
   - SpriteRenderer（设置子弹外观）
   - Collider2D（设置为Trigger）
   - Rigidbody2D（可选，用于物理模拟）
   - **EnemyBullet**脚本
3. 配置EnemyBullet参数
4. 将预制体保存到Resources/Prefab/Bullet/目录

### 4. 代码配置示例

```csharp
// 创建近战攻击行为
MeleeAttackBehavior meleeBehavior = AttackBehaviorFactory.CreateMeleeAttack(
    damage: 25f, 
    cooldown: 0.8f, 
    range: 1.2f
);

// 创建远程攻击行为
RangedAttackBehavior rangedBehavior = AttackBehaviorFactory.CreateRangedAttack(
    damage: 18f, 
    cooldown: 1.2f, 
    range: 6f, 
    bulletSpeed: 10f, 
    bulletPrefab: bulletPrefab
);
```

## 扩展新攻击类型

### 1. 创建新的攻击行为类

```csharp
[CreateAssetMenu(fileName = "New Attack", menuName = "Tower Defense/Attack Behaviors/New Attack")]
public class NewAttackBehavior : ScriptableObject, IAttackBehavior
{
    [Header("新攻击配置")]
    [SerializeField] private float damage = 30f;
    [SerializeField] private float cooldown = 2f;
    
    public void PerformAttack(EnemyController attacker, GameObject target)
    {
        // 实现具体的攻击逻辑
    }
    
    public bool CanAttack(EnemyController attacker, GameObject target)
    {
        // 实现攻击条件检查
        return true;
    }
    
    public float GetAttackCooldown()
    {
        return cooldown;
    }
}
```

### 2. 在工厂中添加创建方法

```csharp
public static NewAttackBehavior CreateNewAttack(float damage = 30f, float cooldown = 2f)
{
    NewAttackBehavior behavior = ScriptableObject.CreateInstance<NewAttackBehavior>();
    // 设置参数
    return behavior;
}
```

## 注意事项

1. **子弹预制体**：远程攻击需要配置子弹预制体，预制体必须包含EnemyBullet组件
2. **动画系统**：攻击行为会自动触发动画，确保敌人有Animator组件和相应的动画状态
3. **音效系统**：攻击行为会自动播放音效，确保敌人有AudioSource组件
4. **特效系统**：可以配置命中特效，特效会自动在目标位置生成并销毁
5. **性能优化**：攻击行为使用ScriptableObject，多个敌人可以共享同一个攻击行为实例

## 调试功能

- 在Scene视图中，选中的敌人会显示攻击范围（红色圆圈）
- 使用EnemyAttackBehaviorExample脚本可以测试攻击行为
- 在Console中会输出详细的攻击日志

## Unity配置要求

1. 确保敌人GameObject有正确的标签（"Enemy"）
2. 确保塔GameObject有正确的标签（"Tower"或"CenterTower"）
3. 确保目标有DamageTaker组件来接收伤害
4. 子弹预制体需要正确设置碰撞器（Trigger模式） 