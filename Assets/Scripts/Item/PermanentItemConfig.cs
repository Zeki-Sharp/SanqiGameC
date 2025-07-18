
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPermanentItemConfig", menuName = "Tower Defense/Item/PermanentItemConfig")]
public class PermanentItemConfig : ItemConfig
{
    public List<ItemData<TowerStatType>> ItemDatas = new List<ItemData<TowerStatType>>();
    public override void Init()
    {
    }

    public override void Use()
    {
        if (!ValidateConfig()) return;
        foreach (var data in ItemDatas)
        {
            // BattleManager.Instance.ApplyTemporaryBuff(data.type, data.valueType, data.value);
            Debug.Log($"已触发临时战斗增益：{ItemName} - {data.type} +{data. value}{(data.valueType == ValueType.Absolute ? "" : "%")}");
        }   
    }
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