using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Data", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("基础信息")]
    [SerializeField] private int id;
    [SerializeField] private string towerName;
    
    [Header("生命值")]
    [SerializeField] private float health;
    
    [Header("攻击属性")]
    [SerializeField] private float attackRange;
    [SerializeField] private float attackInterval;
    [SerializeField] private float physicAttack;
    [SerializeField] private float magicAttack;
    
    [Header("暴击属性")]
    [SerializeField] private float criticalHitRate;
    [SerializeField] private float criticalHitMulti;
    
    [Header("防御属性")]
    [SerializeField] private float physicDefense;
    [SerializeField] private float magicDefense;
    
    // 公共属性访问器
    public int ID => id;
    public string TowerName => towerName;
    public float Health => health;
    public float AttackRange => attackRange;
    public float AttackInterval => attackInterval;
    public float PhysicAttack => physicAttack;
    public float MagicAttack => magicAttack;
    public float CriticalHitRate => criticalHitRate;
    public float CriticalHitMulti => criticalHitMulti;
    public float PhysicDefense => physicDefense;
    public float MagicDefense => magicDefense;
}
