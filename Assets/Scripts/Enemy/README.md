# 敌人系统使用说明

## 系统架构

这个敌人系统采用了状态机模式，包含以下核心组件：

### 1. 状态机架构
- **EnemyState**: 状态基类，定义状态的基本接口
- **EnemyMoveState**: 移动状态，寻找centerTower标签物体并朝向其移动
- **EnemyAttackState**: 攻击状态，在攻击范围内时对塔进行攻击

### 2. 控制器
- **EnemyController**: 敌人主控制器，管理状态切换和敌人行为

### 3. 数据配置
- **EnemyData**: ScriptableObject，用于配置敌人属性
- **EnemySpawner**: 敌人生成器，管理敌人的生成

## 使用方法

### 1. 创建敌人数据
1. 在Project窗口中右键 → Create → Game → Enemy Data
2. 配置敌人的基础属性（生命值、移动速度、攻击范围等）
3. 设置敌人的精灵图片和预制体（可选）

### 2. 设置场景
1. 确保场景中有标签为"centerTower"的塔对象
2. 创建一个空GameObject，添加EnemySpawner组件
3. 将创建的EnemyData拖拽到EnemySpawner的Enemy Data字段
4. 设置生成点位置和生成参数

### 3. 状态切换逻辑
- **初始状态**: 敌人生成时自动进入Move状态
- **Move → Attack**: 当检测到攻击范围内有centerTower标签物体时
- **Attack → Move**: 当攻击范围内没有centerTower标签物体时

## 配置参数

### EnemyController参数
- **Attack Range**: 攻击范围（默认1.5）
- **Move Speed**: 移动速度（默认2）
- **Max Health**: 最大生命值（默认100）
- **Show Debug Info**: 是否显示调试信息

### EnemySpawner参数
- **Enemy Data**: 敌人数据配置
- **Spawn Point**: 生成点位置
- **Spawn Interval**: 生成间隔（秒）
- **Max Enemies**: 最大敌人数
- **Auto Spawn**: 是否自动生成

## 扩展攻击逻辑

EnemyAttackState中的PerformAttack方法是抽象方法，可以继承该类来实现具体的攻击逻辑：

```csharp
public class CustomEnemyAttackState : EnemyAttackState
{
    public CustomEnemyAttackState(EnemyController controller) : base(controller) { }
    
    protected override void PerformAttack()
    {
        // 实现具体的攻击逻辑
        // 例如：播放动画、造成伤害、生成特效等
    }
}
```

## 调试功能

- 在Scene视图中选中敌人可以看到攻击范围（红色圆圈）
- 敌人头顶会显示当前状态和生命值信息
- 生成点会显示绿色圆圈和黄色方向箭头

## 注意事项

1. 确保场景中有标签为"centerTower"的物体
2. 敌人会自动寻找最近的centerTower作为目标
3. 攻击逻辑需要根据具体需求进行扩展
4. 可以通过继承EnemyState来添加新的状态类型 