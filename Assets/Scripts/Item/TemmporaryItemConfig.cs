
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTemmporaryItemConfig", menuName = "Tower Defense/Item/TemmporaryItemConfig")]
public class TemmporaryItemConfig : ItemConfig
{
    public class TemmporaryData
    {
        public BattleBuffType type;
        public ValueType valueType;
        [Range(-100, 100)]
        public float value;
    }
    public List<TemmporaryData> TemmporaryDatas = new List<TemmporaryData>();
    public override void Use()
    {
        if (!ValidateConfig()) return;
        
        // 实现临时增益逻辑
        // 例如：BattleManager.Instance.ApplyTemporaryBuff(BuffType, BuffValue);
        // Debug.Log($"已触发临时战斗增益：{ItemName} - {BuffType} +{BuffValue}%");
    }
}

/// <summary>
/// 战斗增益类型枚举，用于标识临时道具效果
/// 符合道具系统设计规范的效果配置要求
/// </summary>
public enum BattleBuffType
{
    None,           // 无效类型（用于初始化检测）
    DamageBoost,    // 伤害提升
    AttackSpeedUp,  // 攻速提升
    ArmorPiercing,  // 穿透增强
    HealEffect,     // 治疗效果
    ShieldPower,    // 护盾强度
    CritialChance,  // 暴击概率
    StrangeRate     //出怪率
}
/// <summary>
/// 塔属性类型枚举，用于标识可强化的塔属性
/// 符合游戏对象升级系统设计规范的参数验证要求
/// </summary>
public enum TowerStatType
{
    None,           // 无效类型（用于初始化检测）
    AttackPower,    // 攻击力
    AttackSpeed,    // 攻击速度
    Range,          // 攻击范围
    Defense,        // 防御力
    Health,         // 生命值
    CriticalRate,   // 暴击率
    CriticalDamage  // 暴击伤害
}

public enum ValueType
{
    Percent,
    Absolute
}