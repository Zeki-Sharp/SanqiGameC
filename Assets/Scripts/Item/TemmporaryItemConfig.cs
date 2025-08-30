
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTemmporaryItemConfig", menuName = "Tower Defense/Item/TemmporaryItemConfig")]
public class TemmporaryItemConfig : ItemConfig
{
    public List<ItemData<BattleBuffType>> ItemDatas = new List<ItemData<BattleBuffType>>();

    public override void Init()
    {
    }

    public override void Use()
    {
        if (!ValidateConfig()) return;
        
        // 实现临时增益逻辑
        // 例如：BattleManager.Instance.ApplyTemporaryBuff(BuffType, BuffValue);
        foreach (var data in ItemDatas)
        {
            // EventBus.Instance.Publish( new TowerBuffEventArgs(data.type, data.valueType, data.value));
            // BattleManager.Instance.ApplyTemporaryBuff(data.type, data.valueType, data.value);
             Debug.Log($"已触发临时战斗增益：{ItemName} - {data.type} +{data. value}{(data.valueType == ValueType.Absolute ? "" : "%")}");
        }
       
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


public enum ValueType
{
    Percent,
    Absolute
}