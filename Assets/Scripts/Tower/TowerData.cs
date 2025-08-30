using System;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;

// 治疗效果类型枚举
public enum HealEffectType
{
    Instant,     // 瞬间治疗：一次性恢复固定数值生命值
    OverTime,    // 持续治疗：在指定时间内持续恢复生命值
}

// 治疗范围类型枚举
public enum HealRangeType
{
    None,        // 无范围治疗
    Adjacent4    // 周围四格（上下左右）
}

// 技能类型枚举
public enum SkillType
{
    None,        // 无技能（默认空技能）
    Attack,      // 攻击技能
    Heal         // 治疗技能
}

/// <summary>
/// 技能配置基类
/// </summary>
[Serializable]
public abstract class SkillConfig
{
    [Header("技能基础信息")]
    [SerializeField, LabelText("技能名称")] private string skillName = "新技能";
    [SerializeField, LabelText("技能描述")] private string skillDescription = "";
    
    public string SkillName => skillName;
    public string SkillDescription => skillDescription;
    public abstract SkillType Type { get; }
}

/// <summary>
/// 空技能配置（用于表示未选择的技能槽位）
/// </summary>
[Serializable]
public class EmptySkill : SkillConfig
{
    public override SkillType Type => SkillType.None;
    
    public override string ToString()
    {
        return "空技能槽";
    }
}

/// <summary>
/// 攻击技能配置（继承自技能配置基类）
/// </summary>
[Serializable]
public class AttackSkill : SkillConfig
{
    [Header("攻击参数")]
    [SerializeField, LabelText("物理攻击")] private float physicAttack = 100f;
    [SerializeField, LabelText("攻击范围")] private float attackRange = 3f;
    [SerializeField, LabelText("攻击间隔")] private float attackInterval = 1f;
    [SerializeField, LabelText("攻击速度")] private float attackSpeed = 1f;

    public override SkillType Type => SkillType.Attack;
    
    // 公共属性访问器
    public float PhysicAttack => physicAttack;
    public float AttackRange => attackRange;
    public float AttackInterval => attackInterval;
    public float AttackSpeed => attackSpeed;
}

/// <summary>
/// 治疗技能配置（继承自技能配置基类）
/// </summary>
[Serializable]
public class HealSkill : SkillConfig
{
    [Header("治疗参数")]
    [SerializeField, LabelText("治疗量")] private float healAmount = 50f;
    [SerializeField, LabelText("治疗间隔")] private float healInterval = 2f;
    [SerializeField, LabelText("治疗范围类型")] private HealRangeType healRangeType = HealRangeType.Adjacent4;
    [SerializeField, LabelText("治疗效果类型")] private HealEffectType healEffectType = HealEffectType.Instant;
    [SerializeField, LabelText("最大治疗目标数")] private int maxHealTargets = 5;
    
    public override SkillType Type => SkillType.Heal;
    
    // 公共属性访问器
    public float HealAmount => healAmount;
    public float HealInterval => healInterval;
    public HealRangeType HealRangeType => healRangeType;
    public HealEffectType HealEffectType => healEffectType;
    public int MaxHealTargets => maxHealTargets;
}

/// <summary>
/// 技能槽位配置 - 支持动态选择技能类型
/// </summary>
[Serializable]
public class SkillSlot
{
    [Header("技能选择")]
    [SerializeField, LabelText("技能类型"), OnValueChanged("OnSkillTypeChanged")]
    private SkillType skillType = SkillType.None;
    
    [Header("技能配置")]
    [ShowIf("skillType", SkillType.Attack)]
    [SerializeField, LabelText("攻击技能配置")] private AttackSkill attackSkill;
    
    [ShowIf("skillType", SkillType.Heal)]
    [SerializeField, LabelText("治疗技能配置")] private HealSkill healSkill;
    
    // 公共属性
    public SkillType SkillType => skillType;
    public AttackSkill AttackSkill => attackSkill;
    public HealSkill HealSkill => healSkill;
    
    // 获取当前技能配置
    public SkillConfig GetCurrentSkill()
    {
        switch (skillType)
        {
            case SkillType.Attack:
                return attackSkill;
            case SkillType.Heal:
                return healSkill;
            default:
                return null;
        }
    }
    
    // 技能类型改变时的处理
    private void OnSkillTypeChanged()
    {
        // 根据选择的技能类型创建对应的技能配置
        switch (skillType)
        {
            case SkillType.Attack:
                if (attackSkill == null)
                    attackSkill = new AttackSkill();
                healSkill = null; // 清空其他类型的技能
                break;
            case SkillType.Heal:
                if (healSkill == null)
                    healSkill = new HealSkill();
                attackSkill = null; // 清空其他类型的技能
                break;
            default:
                attackSkill = null;
                healSkill = null;
                break;
        }
    }
    
    // 检查是否有有效技能
    public bool HasValidSkill => skillType != SkillType.None && GetCurrentSkill() != null;
    
    // 获取技能名称（用于显示）
    public string GetSkillDisplayName()
    {
        if (skillType == SkillType.None)
            return "空技能槽";
        
        var skill = GetCurrentSkill();
        return skill?.SkillName ?? "未配置";
    }
}

[Serializable]
public class TowerLevel
{
    [PreviewField(150)]
    [SerializeField, LabelText("塔图片")] private Sprite towerSprite;
    [SerializeField, LabelText("生命值")] private float health;
    
    // 统一的技能配置 - 动态技能列表
    [Header("技能配置")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "GetSkillDisplayName", 
                       CustomAddFunction = "AddEmptySkillSlot")]
    [SerializeField, LabelText("技能列表")] private List<SkillSlot> skillSlots = new List<SkillSlot>();
    
    // 基础属性访问器
    public Sprite TowerSprite => towerSprite;
    public float Health => health;
    
    // 技能相关访问器
    public List<SkillSlot> SkillSlots => skillSlots;
    
    // 获取所有技能
    public List<SkillConfig> GetAllSkills()
    {
        var allSkills = new List<SkillConfig>();
        foreach (var slot in skillSlots)
        {
            if (slot.HasValidSkill)
            {
                allSkills.Add(slot.GetCurrentSkill());
            }
        }
        return allSkills;
    }
    
    // 获取指定类型的技能
    public List<AttackSkill> GetAttackSkills()
    {
        var attackSkills = new List<AttackSkill>();
        foreach (var slot in skillSlots)
        {
            if (slot.SkillType == SkillType.Attack && slot.AttackSkill != null)
            {
                attackSkills.Add(slot.AttackSkill);
            }
        }
        return attackSkills;
    }
    
    public List<HealSkill> GetHealSkills()
    {
        var healSkills = new List<HealSkill>();
        foreach (var slot in skillSlots)
        {
            if (slot.SkillType == SkillType.Heal && slot.HealSkill != null)
            {
                healSkills.Add(slot.HealSkill);
            }
        }
        return healSkills;
    }
    
    // 检查是否有指定类型的技能
    public bool HasAttackSkill => GetAttackSkills().Count > 0;
    public bool HasHealSkill => GetHealSkills().Count > 0;
    
    // 便捷访问器 - 保持与原有代码的兼容性（取第一个技能的数据）
    public float PhysicAttack => GetAttackSkills().Count > 0 ? GetAttackSkills()[0].PhysicAttack : 0f;
    public float AttackRange => GetAttackSkills().Count > 0 ? GetAttackSkills()[0].AttackRange : 0f;
    public float AttackInterval => GetAttackSkills().Count > 0 ? GetAttackSkills()[0].AttackInterval : 0f;
    public float AttackSpeed => GetAttackSkills().Count > 0 ? GetAttackSkills()[0].AttackSpeed : 0f;
    
    public float HealAmount => GetHealSkills().Count > 0 ? GetHealSkills()[0].HealAmount : 0f;
    public float HealInterval => GetHealSkills().Count > 0 ? GetHealSkills()[0].HealInterval : 0f;
    public HealRangeType HealRangeType => GetHealSkills().Count > 0 ? GetHealSkills()[0].HealRangeType : HealRangeType.None;
    public HealEffectType HealEffectType => GetHealSkills().Count > 0 ? GetHealSkills()[0].HealEffectType : HealEffectType.Instant;
    public int MaxHealTargets => GetHealSkills().Count > 0 ? GetHealSkills()[0].MaxHealTargets : 0;
    
    // Odin Inspector 自定义添加函数 - 添加空技能槽位
    private SkillSlot AddEmptySkillSlot()
    {
        return new SkillSlot();
    }
}

[CreateAssetMenu(fileName = "New Tower Data", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("基础信息")]
    [SerializeField] private int id;
    [SerializeField] private string towerName;
    [TextArea]
    [SerializeField] private string towerDescription;
    [PreviewField(150)]
    [SerializeField] private Sprite towerSprite;
    
    [Header("等级数据")]
    [SerializeField] private TowerLevel[] levels = new TowerLevel[0];

    [Header("子弹配置"), InlineEditor(InlineEditorModes.GUIOnly, InlineEditorObjectFieldModes.Hidden)]
    [SerializeField] private BulletConfig bulletConfig;

    // 公共属性访问器
    public int ID => id;
    public string TowerName => towerName;
    public string TowerDescription => towerDescription;
    public Sprite TowerSprite => towerSprite;
    
    public int MaxLevel => levels != null ? levels.Length : 0;

    public Sprite GetTowerSprite(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return null;
        }
        
        Sprite result = levels[level].TowerSprite;
        Debug.Log($"[TowerData Debug] 获取等级 {level} 的图片: {result?.name ?? "null"}");
        return result;
    }
    
    public float GetHealth(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return 0;
        }
        return levels[level].Health;
    }
    
    public float GetAttackRange(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return 0;
        }
        return levels[level].AttackRange;
    }

    public float GetAttackInterval(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return 0;
        }
        return levels[level].AttackInterval;
    }
    
    public float GetPhysicAttack(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return 0;
        }
        return levels[level].PhysicAttack;
    }

    public float GetAttackSpeed(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return 0;
        }
        return levels[level].AttackSpeed;
    }
    
    // 治疗技能访问器
    public float GetHealAmount(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return 0;
        }
        return levels[level].HealAmount;
    }
    
    public float GetHealInterval(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return 0;
        }
        return levels[level].HealInterval;
    }
    
    public HealRangeType GetHealRangeType(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return HealRangeType.None;
        }
        return levels[level].HealRangeType;
    }
    
    public HealEffectType GetHealEffectType(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return HealEffectType.Instant;
        }
        return levels[level].HealEffectType;
    }
    
    /// <summary>
    /// 获取子弹配置
    /// </summary>
    public BulletConfig GetBulletConfig()
    {
        return bulletConfig;
    }
    
    /// <summary>
    /// 获取指定等级的完整数据
    /// </summary>
    public TowerLevel GetLevelData(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return new TowerLevel();
        }
        return levels[level];
    }
    
    /// <summary>
    /// 检查指定等级是否有攻击技能
    /// </summary>
    public bool HasAttackSkill(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            return false;
        }
        return levels[level].HasAttackSkill;
    }
    
    /// <summary>
    /// 检查指定等级是否有治疗技能
    /// </summary>
    public bool HasHealSkill(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            return false;
        }
        return levels[level].HasHealSkill;
    }
}