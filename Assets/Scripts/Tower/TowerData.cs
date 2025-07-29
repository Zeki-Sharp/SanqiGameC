using System;
using UnityEngine;

[Serializable]
public struct TowerLevel
{
    [SerializeField] private Sprite towerSprite;
    [SerializeField] private float health;
    [SerializeField] private float physicAttack;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackInterval;
    // [SerializeField] private float magicAttack;
    [SerializeField] private float attackSpeed;
    // [SerializeField] private float physicDefense;
    // [SerializeField] private float magicDefense;

    public float Health => health;
    public float AttackRange => attackRange;
    public float AttackInterval => attackInterval; 
    public float PhysicAttack => physicAttack;
    // public float MagicAttack => magicAttack;
    public float AttackSpeed => attackSpeed;
    // public float PhysicDefense => physicDefense;
    // public float MagicDefense => magicDefense;
}

[CreateAssetMenu(fileName = "New Tower Data", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("基础信息")]
    [SerializeField] private int id;
    [SerializeField] private string towerName;
    [SerializeField] private Sprite towerSprite;
    
    [Header("等级数据")]
[SerializeField] private TowerLevel[] levels = new TowerLevel[0];

[Header("子弹配置")]
[SerializeField] private BulletConfig bulletConfig;

    // 公共属性访问器
    public int ID => id;
    public string TowerName => towerName;
    public Sprite TowerSprite => towerSprite;
    
    public int MaxLevel => levels != null ? levels.Length : 0;

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

    // public float GetMagicAttack(int level)
    // {
    //     if (levels == null || level < 0 || level >= levels.Length)
    //     {
    //         Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
    //         return 0;
    //     }
    //     return levels[level].MagicAttack;
    // }

    public float GetAttackSpeed(int level)
    {
        if (levels == null || level < 0 || level >= levels.Length)
        {
            Debug.LogError($"无效等级 {level}，等级范围应为 0-{levels?.Length - 1 ?? 0}");
            return 0;
        }
        return levels[level].AttackSpeed;
    }
    
    /// <summary>
    /// 获取子弹配置
    /// </summary>
    public BulletConfig GetBulletConfig()
    {
        return bulletConfig;
    }

  
}